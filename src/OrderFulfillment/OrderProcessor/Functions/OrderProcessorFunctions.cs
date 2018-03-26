using System;
using System.Collections.Generic;
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
using OrderFulfillment.OrderProcessor.Infrastructure;
using Newtonsoft.Json;
using NodaTime;
using Serilog;

namespace OrderFulfillment.OrderProcessor.Functions
{
    /// <summary>
    ///   The set of WebJob-triggered functions for the order processor.
    /// </summary>
    /// 
    /// <seealso cref="OrderFulfillment.Core.WebJobs.WebJobFunctionBase" />
    /// 
    public class OrderProcessorFunctions : WebJobFunctionBase
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
        private readonly IOrderProcessor orderProcessor;

        // <summary>The publisher to use for sending of the <see cref="ProcessOrder" /> command</summary>
        private readonly ICommandPublisher<ProcessOrder> processOrderPublisher;

        /// <summary>The publisher to use for sending of the <see cref="SubmitOrderForProduction" /> command</summary>
        private readonly ICommandPublisher<SubmitOrderForProduction> submitOrderForProductionPublisher;

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
        ///   Initializes a new instance of the <see cref="OrderProcessorFunctions"/> class.
        /// </summary>
        /// 
        /// <param name="jsonSerializer">The JSON serializer to use for parsing messages from the queue.</param>
        /// <param name="retryThresholds">The thresholds for use when retrying commands on a backoff policy.</param>
        /// <param name="clock">The clock to use for time-realated operations.</param>
        /// <param name="orderProcessor">The processor to use for the order associated with a ProcesOrder command,</param>
        /// <param name="processOrderPublisher">The publisher to use for sending of the <see cref="ProcessOrder" /> command</param>
        /// <param name="submitOrderForProductionPublisher">The publisher to use for sending of the <see cref="SubmitOrderForProduction" /> command</param>
        /// <param name="notifyOfFatalFailurePublisher">The publisher to use for sending of the <see cref="NotifyOfFatalFailure" /> command.</param>
        /// <param name="eventPublisher">The publisher to use for the sending of events.</param>
        /// <param name="logger">The logger to be used for emitting telemetry from the controller.</param>
        /// <param name="lifetimeScope">The lifetime scope associated with the class; this will be disposed when the class is by the base class..</param>
        /// 
        public OrderProcessorFunctions(JsonSerializer                              jsonSerializer,
                                       CommandRetryThresholds                      retryThresholds,
                                       IClock                                      clock,
                                       IOrderProcessor                             orderProcessor,
                                       ICommandPublisher<ProcessOrder>             processOrderPublisher,
                                       ICommandPublisher<SubmitOrderForProduction> submitOrderForProductionPublisher,
                                       ICommandPublisher<NotifyOfFatalFailure>     notifyOfFatalFailurePublisher,
                                       IEventPublisher<EventBase>                  eventPublisher,
                                       ILogger                                     logger,                                       
                                       IDisposable                                 lifetimeScope) : base(lifetimeScope)
        {
            this.jsonSerializer                    = jsonSerializer                    ?? throw new ArgumentNullException(nameof(jsonSerializer));
            this.retryThresholds                   = retryThresholds                   ?? throw new ArgumentNullException(nameof(retryThresholds));
            this.clock                             = clock                             ?? throw new ArgumentNullException(nameof(clock));
            this.orderProcessor                    = orderProcessor                    ?? throw new ArgumentNullException(nameof(orderProcessor));
            this.processOrderPublisher             = processOrderPublisher             ?? throw new ArgumentNullException(nameof(processOrderPublisher));
            this.submitOrderForProductionPublisher = submitOrderForProductionPublisher ?? throw new ArgumentNullException(nameof(submitOrderForProductionPublisher));
            this.notifyOfFatalFailurePublisher     = notifyOfFatalFailurePublisher     ?? throw new ArgumentNullException(nameof(notifyOfFatalFailurePublisher));
            this.eventPublisher                    = eventPublisher                    ?? throw new ArgumentNullException(nameof(eventPublisher));            
            this.Log                               = logger                            ?? throw new ArgumentNullException(nameof(logger));

            this.rng = new Random();
        }

        /// <summary>
        ///   Performs the tasks needed to handle a <see cref="ProcessOrder" /> command.
        /// </summary>
        /// 
        /// <param name="processOrderMessage">The brokered message containing the command to process.</param>
        /// 
        public async Task HandleProcessOrderAsync([ServiceBusTrigger(TriggerQueueNames.ProcessOrderCommandQueue)] BrokeredMessage processOrderMessage)
        {            
            if (processOrderMessage == null)
            {   
                this.Log.Error("The {CommandType} brokered message was null.", nameof(ProcessOrder));
                throw new ArgumentNullException(nameof(processOrderMessage));
            }

            ILogger      log     = null;
            ProcessOrder command = null;           

            try
            {
                // Attempt to retrieve the command from the brokered message.

                using (var bodyStream = processOrderMessage.GetBody<Stream>())
                using (var reader     = new StreamReader(bodyStream))
                using (var jsonReader = new JsonTextReader(reader))
                {   
                    command = this.jsonSerializer.Deserialize<ProcessOrder>(jsonReader);
                                
                    jsonReader.Close();
                    reader.Close();
                    bodyStream.Close();
                };

                // If the body was not a proper ProcessOrder command, an empty command will be 
                // returned.  Verify that the Id and OrderId are not in their default states.

                if ((command.Id == Guid.Empty) && (command.OrderId == null))
                {
                    throw new MissingDependencyException("The command could not be extracted from the brokered message");
                }

                // Process the order identified by the command.
                
                var correlationId = command.CorrelationId ?? processOrderMessage.CorrelationId;

                log = this.Log.WithCorrelationId(correlationId);
                log.Information("A {CommandType} command was received and is being handled.  {Command}", nameof(ProcessOrder), command);

                var result = await this.orderProcessor.ProcessOrderAsync(command.PartnerCode, command.OrderId, (IReadOnlyDictionary<string, string>)command.Assets, command.Emulation, correlationId);

                // Consider the result to complete handling.  If processing was successful, then the message should be completed to
                // ensure that it is not retried.

                if (result?.Outcome == Outcome.Success)
                {
                    await this.submitOrderForProductionPublisher.PublishAsync(command.CreateNewOrderCommand<SubmitOrderForProduction>(cmd => cmd.Emulation = command.Emulation));                    
                    await this.CompleteMessageAsync(processOrderMessage);

                    this.eventPublisher.TryPublishAsync(command.CreateNewOrderEvent<OrderProcessed>()).FireAndForget();
                    log.Information("A {CommandType} command was successfully handled.  The order was staged for submission with the key {CreateOrderMessageKey}.", nameof(ProcessOrder), result?.Payload);
                }
                else
                {
                    log.Warning("A {CommandType} command was successfully handled.  The order procesing was not successful, however.", nameof(ProcessOrder));

                    // Attempt to schedule the command for a retry using a backoff policy.  If scheduled, complete the current message so that it is removed 
                    // from the queue.  Otherwise, throw to expose the failure to the WebJob infrastructure so that the command will be moved to the dead letter
                    // queue after retries are exhausted.

                    if (await this.ScheduleCommandForRetryIfEligibleAsync(command, this.retryThresholds, this.rng, this.clock, this.processOrderPublisher))
                    {
                        await this.CompleteMessageAsync(processOrderMessage);
                    }
                    else
                    {
                        await this.notifyOfFatalFailurePublisher.TryPublishAsync(command.CreateNewOrderCommand<NotifyOfFatalFailure>());
                        throw new FailedtoHandleCommandException();
                    }
                }
            }

            catch (Exception ex) when (!(ex is FailedtoHandleCommandException))
            {
                (log ?? this.Log).Error(ex, "An exception occurred while handling the {CommandType} comand", nameof(ProcessOrder));
                             
                var failedEvent = (command?.CreateNewOrderEvent<OrderProcessingFailed>() ?? new OrderProcessingFailed 
                { 
                   Id              = Guid.NewGuid(),
                   CorrelationId   = processOrderMessage?.CorrelationId,
                   OccurredTimeUtc = this.clock.GetCurrentInstant().ToDateTimeUtc()
                });
                                
                this.eventPublisher.TryPublishAsync(failedEvent).FireAndForget();
                
                // Attempt to schedule the command for a retry using a backoff policy.  If scheduled, complete the current message so that it is removed 
                // from the queue.  Otherwise, throw to expose the failure to the WebJob infrastructure so that the command will be moved to the dead letter
                // queue after retries are exhausted.

                if (await this.ScheduleCommandForRetryIfEligibleAsync(command, this.retryThresholds, this.rng, this.clock, this.processOrderPublisher))
                {
                    await this.CompleteMessageAsync(processOrderMessage);
                }
                else
                {
                    await this.notifyOfFatalFailurePublisher.TryPublishAsync(command.CreateNewOrderCommand<NotifyOfFatalFailure>());
                    throw;
                };
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
