using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;
using OrderFulfillment.Core.Commands;
using OrderFulfillment.Core.Events;
using OrderFulfillment.Core.Exceptions;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Infrastructure;
using OrderFulfillment.Core.Models.Operations;
using OrderFulfillment.Core.WebJobs;
using OrderFulfillment.Notifier.Infrastructure;
using Newtonsoft.Json;
using NodaTime;
using Serilog;

namespace OrderFulfillment.Notifier.Functions
{
    /// <summary>
    ///   The set of WebJob-triggered functions for the order submitter.
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Core.WebJobs.WebJobFunctionBase" />
    /// 
    public class NotifierFunctions : WebJobFunctionBase
    {
        /// <summary>The generator to use for random numbers.</summary>
        private readonly Random rng;

        /// <summary>The serializer to use for JSON serialization/deserializerio</summary>
        private readonly JsonSerializer jsonSerializer;

        /// <summary>The thresholds for use when retrying commands on a backoff policy</summary>
        private readonly CommandRetryThresholds retryThresholds;

        /// <summary>The clock to use for time-realated operations/summary>
        private readonly IClock clock;

        /// <summary>The processor to use for the order associated with a ProcesOrder command</summary>
        private readonly INotifier notifier;

        /// <summary>The publisher to use for sending of the <see cref="NotifyOfFatalFailure" /> command</summary>
        private readonly ICommandPublisher<NotifyOfFatalFailure> notifyOfFatalFailurePublisher;

        /// <summary>The publisher to use for the sending of events</summary>
        private readonly IEventPublisher<EventBase> eventPublisher;

        /// <summary>
        ///   The logger to be used for emitting telemetry.
        /// </summary>
        /// 
        private ILogger Log { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="NotifierFunctions"/> class.
        /// </summary>
        /// 
        /// <param name="jsonSerializer">The JSON serializer to use for parsing messages from the queue.</param>
        /// <param name="retryThresholds">The thresholds for use when retrying commands on a backoff policy.</param>
        /// <param name="clock">The clock to use for time-realated operations.</param>
        /// <param name="notifier">The notifier to use for sending notifications.</param>
        /// <param name="notifyOfFatalFailurePublisher">The publisher to use for sending of the <see cref="NotifyOfFatalFailure" /> command.</param>
        /// <param name="eventPublisher">The publisher to use for the sending of events.</param>
        /// <param name="logger">The logger to be used for emitting telemetry from the controller.</param>
        /// <param name="lifetimeScope">The lifetime scope associated with the class; this will be disposed when the class is by the base class.</param>
        /// 
        public NotifierFunctions(JsonSerializer                          jsonSerializer,
                                 CommandRetryThresholds                  retryThresholds,
                                 IClock                                  clock,
                                 INotifier                               notifier,
                                 ICommandPublisher<NotifyOfFatalFailure> notifyOfFatalFailurePublisher,
                                 IEventPublisher<EventBase>              eventPublisher,
                                 ILogger                                 logger,
                                 IDisposable                             lifetimeScope) : base(lifetimeScope)
        {
            this.jsonSerializer                = jsonSerializer                ?? throw new ArgumentNullException(nameof(jsonSerializer));
            this.retryThresholds               = retryThresholds               ?? throw new ArgumentNullException(nameof(retryThresholds));
            this.clock                         = clock                         ?? throw new ArgumentNullException(nameof(clock));
            this.notifier                      = notifier                      ?? throw new ArgumentNullException(nameof(notifier));
            this.notifyOfFatalFailurePublisher = notifyOfFatalFailurePublisher ?? throw new ArgumentNullException(nameof(notifyOfFatalFailurePublisher));
            this.eventPublisher                = eventPublisher                ?? throw new ArgumentNullException(nameof(eventPublisher));            
            this.Log                           = logger                        ?? throw new ArgumentNullException(nameof(logger));

            this.rng = new Random();
        }

        /// <summary>
        ///   Performs the tasks needed to handle a <see cref="NotifyOfFatalFailure" /> command.
        /// </summary>
        /// 
        /// <param name="notifyOfFatalFailureMessage">The brokered message containing the command to process.</param>
        /// 
        public async Task HandleNotifyOfFatalFailureAsync([ServiceBusTrigger(TriggerQueueNames.NotifyOfFatalFailureCommandQueue)] BrokeredMessage notifyOfFatalFailureMessage)
        {            
            if (notifyOfFatalFailureMessage == null)
            {   
                this.Log.Error("The {CommandType} brokered message was null.", nameof(NotifyOfFatalFailure));
                throw new ArgumentNullException(nameof(notifyOfFatalFailureMessage));
            }

            ILogger              log     = null;
            NotifyOfFatalFailure command = null;           

            try
            {
                // Attempt to retrieve the command from the brokered message.

                using (var bodyStream = notifyOfFatalFailureMessage.GetBody<Stream>())
                using (var reader     = new StreamReader(bodyStream))
                using (var jsonReader = new JsonTextReader(reader))
                {   
                    command = this.jsonSerializer.Deserialize<NotifyOfFatalFailure>(jsonReader);
                                
                    jsonReader.Close();
                    reader.Close();
                    bodyStream.Close();
                };

                // If the body was not a proper NotifyOfFatalFailure command, an empty command will be 
                // returned.  Verify that the Id and OrderId are not in their default states.

                if ((command.Id == Guid.Empty) && (command.OrderId == null))
                {
                    throw new MissingDependencyException("The command could not be extracted from the brokered message");
                }

                // Process the order identified by the command.
                
                var correlationId = command.CorrelationId ?? notifyOfFatalFailureMessage.CorrelationId;

                log = this.Log.WithCorrelationId(correlationId);
                log.Information("A {CommandType} command was received and is being handled.  {Command}", nameof(NotifyOfFatalFailure), command);

                var result = await this.notifier.NotifyOfOrderFailureAsync(command.PartnerCode, command.OrderId, correlationId);

                // Consider the result to complete handling.  If processing was successful, then the message should be completed to
                // ensure that it is not retried.

                if (result?.Outcome == Outcome.Success)
                {
                    await this.CompleteMessageAsync(notifyOfFatalFailureMessage);

                    var sentEvent = (command?.CreateNewOrderEvent<NotificationSent>() ?? new NotificationSent 
                    { 
                        Id              = Guid.NewGuid(),
                        CorrelationId   = correlationId,
                        OccurredTimeUtc = this.clock.GetCurrentInstant().ToDateTimeUtc()
                    });

                    this.eventPublisher.TryPublishAsync(sentEvent).FireAndForget();
                    log.Information("A {CommandType} command was successfully handled.  The fatal notification was sent for order {Partner}//{Order}.", nameof(NotifyOfFatalFailure), command.PartnerCode, command.OrderId);
                }
                else
                {                    
                    log.Warning("A {CommandType} command was successfully handled.  The notification was not successful, however.", nameof(NotifyOfFatalFailure));

                    // Attempt to schedule the command for a retry using a backoff policy.  If scheduled, complete the current message so that it is removed 
                    // from the queue.  Otherwise, throw to expose the failure to the WebJob infrastructure so that the command will be moved to the dead letter
                    // queue after retries are exhausted.

                    if (await this.ScheduleCommandForRetryIfEligibleAsync(command, this.retryThresholds, this.rng, this.clock, this.notifyOfFatalFailurePublisher))
                    {
                        await this.CompleteMessageAsync(notifyOfFatalFailureMessage);
                    }
                    else
                    {
                        throw new FailedtoHandleCommandException();
                    }                    
                }
            }

            catch (Exception ex) when (!(ex is FailedtoHandleCommandException))
            {
                (log ?? this.Log).Error(ex, "An exception occurred while handling the {CommandType} comand", nameof(NotifyOfFatalFailure));
                             
                var failedEvent = (command?.CreateNewOrderEvent<NotificationFailed>() ?? new NotificationFailed 
                { 
                    Id              = Guid.NewGuid(),
                    CorrelationId   = notifyOfFatalFailureMessage?.CorrelationId,
                    OccurredTimeUtc = this.clock.GetCurrentInstant().ToDateTimeUtc()
                });

                this.eventPublisher.TryPublishAsync(failedEvent).FireAndForget();
                                
                // Attempt to schedule the command for a retry using a backoff policy.  If scheduled, complete the current message so that it is removed 
                // from the queue.  Otherwise, throw to expose the failure to the WebJob infrastructure so that the command will be moved to the dead letter
                // queue after retries are exhausted.

                if (await this.ScheduleCommandForRetryIfEligibleAsync(command, this.retryThresholds, this.rng, this.clock, this.notifyOfFatalFailurePublisher))
                {
                    await this.CompleteMessageAsync(notifyOfFatalFailureMessage);
                }
                else
                {
                    throw;
                };
            }
        }

        /// <summary>
        ///   Performs the tasks needed to handle a <see cref="ProcessOrder" /> dead letter message.
        /// </summary>
        /// 
        /// <param name="message">The brokered message containing the dead letter command to process.</param>
        /// 
        public async Task HandleProcessOrderDeadLetterAsync([ServiceBusTrigger(TriggerQueueNames.ProcessOrderDeadLetterQueue)] BrokeredMessage message)
        {            
            await this.ProcessDeadLetterMessage(TriggerQueueNames.ProcessOrderDeadLetterQueue, message);
        }

        /// <summary>
        ///   Performs the tasks needed to handle a <see cref="SubmitOrderForProduction" /> dead letter message.
        /// </summary>
        /// 
        /// <param name="message">The brokered message containing the dead letter command to process.</param>
        /// 
        public async Task HandleSubmitOrderForProductionDeadLetterAsync([ServiceBusTrigger(TriggerQueueNames.SubmitOrderForProductionDeadLetterQueue)] BrokeredMessage message)
        {            
            await this.ProcessDeadLetterMessage(TriggerQueueNames.SubmitOrderForProductionDeadLetterQueue, message);
        }

        /// <summary>
        ///   Performs the tasks needed to handle a <see cref="NotifyOfFatalFailure" /> dead letter message.
        /// </summary>
        /// 
        /// <param name="message">The brokered message containing the dead letter command to process.</param>
        /// 
        public async Task HandleNotifyOfFatalFailureDeadLetterQueueDeadLetterAsync([ServiceBusTrigger(TriggerQueueNames.NotifyOfFatalFailureDeadLetterQueue)] BrokeredMessage message)
        {            
            await this.ProcessDeadLetterMessage(TriggerQueueNames.NotifyOfFatalFailureDeadLetterQueue, message);
        }

        /// <summary>
        ///   Performs the tasks needed to process a dead letter message observed on one of the 
        ///   command queues.
        /// </summary>
        /// 
        /// <param name="deadLetterLocation">The name of the location where the dead letter message was observed.  For example, the process-order queue.</param>
        /// <param name="deadLetterMessage">The brokered message containing the command to process.</param>
        /// 
        protected internal virtual async Task ProcessDeadLetterMessage(string          deadLetterLocation,
                                                                       BrokeredMessage deadLetterMessage)
        {            
            if (deadLetterMessage == null)
            {   
                this.Log.Error("The {CommandType} brokered message was null.", "DeadLetter");
                throw new ArgumentNullException(nameof(deadLetterMessage));
            }

            ILogger          log     = null;
            OrderCommandBase command = null;           

            try
            {
                // Attempt to retrieve the command from the brokered message.

                using (var bodyStream = deadLetterMessage.GetBody<Stream>())
                using (var reader     = new StreamReader(bodyStream))
                using (var jsonReader = new JsonTextReader(reader))
                {   
                    command = this.jsonSerializer.Deserialize<OrderCommandBase>(jsonReader);
                                
                    jsonReader.Close();
                    reader.Close();
                    bodyStream.Close();
                };

                // If the body was not a proper OrderCommandBase command, an empty command will be 
                // returned.  Verify that the Id and OrderId are not in their default states.

                if ((command.Id == Guid.Empty) && (command.OrderId == null))
                {
                    throw new MissingDependencyException("The command could not be extracted from the brokered message");
                }

                // Process the order identified by the command.
                
                var correlationId = command.CorrelationId ?? deadLetterMessage.CorrelationId;

                log = this.Log.WithCorrelationId(correlationId);
                log.Information("A {CommandType} command was found on the dead letter {DeadLetterLocation} and notification is being sent.  {Command}", nameof(OrderCommandBase), deadLetterLocation, command);

                var result = await this.notifier.NotifyDeadLetterMessageAsync(deadLetterLocation, command.PartnerCode, command.OrderId, correlationId);

                // Consider the result to complete handling.  If processing was successful, then the message should be completed to
                // ensure that it is not retried.

                if (result?.Outcome == Outcome.Success)
                {
                    await this.CompleteMessageAsync(deadLetterMessage);

                    var sentEvent = (command?.CreateNewOrderEvent<NotificationSent>() ?? new NotificationSent 
                    { 
                        Id              = Guid.NewGuid(),
                        CorrelationId   = correlationId,
                        OccurredTimeUtc = this.clock.GetCurrentInstant().ToDateTimeUtc()
                    });

                    this.eventPublisher.TryPublishAsync(sentEvent).FireAndForget();
                    log.Information("Notification was successfully sent for the {CommandType} found on the dead letter {DeadLetterLocation}.  The fatal notification was sent for order {Partner}//{Order}.", nameof(NotifyOfFatalFailure), deadLetterLocation, command.PartnerCode, command.OrderId);
                }
                else
                {
                    // Submission was not successful.  Throw an exception to signal failure to the WebJob infrastructure 
                    // so that the queue message is retried.
                    //
                    // We should be applying better retry logic and requeuing the message on a delay for a backoff.  This is a future
                    // enhancement.

                    log.Warning("Notification was not sent for the {CommandType} found on the dead letter {DeadLetterLocation}.  The message was handled successfully, but the sending failed", nameof(NotifyOfFatalFailure), deadLetterLocation);
                    throw new FailedtoHandleCommandException();
                }
            }

            catch (Exception ex) when (!(ex is FailedtoHandleCommandException))
            {
                (log ?? this.Log).Error(ex, "An exception occurred while sending notification for the {CommandType} found on the dead letter {DeadLetterLocation}.", nameof(NotifyOfFatalFailure), deadLetterLocation);
                             
                var failedEvent = (command?.CreateNewOrderEvent<NotificationFailed>() ?? new NotificationFailed 
                { 
                    Id              = Guid.NewGuid(),
                    CorrelationId   = deadLetterMessage?.CorrelationId,
                    OccurredTimeUtc = this.clock.GetCurrentInstant().ToDateTimeUtc()
                });
                                
                this.eventPublisher.TryPublishAsync(failedEvent).FireAndForget();
                
                // The exception must bubble in order for the WebJob to mark the failure and perform the actions needed 
                // to fail the message so that it retries.

                throw;
            }
        }

        /// <summary>
        ///   Requests completion of a brokered message.
        /// </summary>
        /// 
        /// <param name="message">The message to request completion of.</param>
        /// 
        /// <remarks>
        ///   This method is intended to serve as a hook for testing, since the 
        ///   BrokeredMessage class is sealed with no interfaces and, therefore, 
        ///   cannot be easily mocked.
        /// </remarks>
        /// 
        protected virtual Task CompleteMessageAsync(BrokeredMessage message)
        {
            return message.CompleteAsync();
        }
    } 
}
