using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using OrderFulfillment.Core.Configuration;
using OrderFulfillment.Core.Extensions;
using OrderFulfillment.Core.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NodaTime;
using Serilog;

namespace OrderFulfillment.Core.Commands
{
    /// <summary>
    ///   Serves as a publsiher for a single command, or family of commands with 
    ///   a common ancestor using Azure Servuce Bus queues as an underlying storage
    ///   mechnaism.
    /// </summary>
    /// 
    /// <typeparam name="T">The type of command capable of being published</typeparam>
    /// 
    /// <seealso cref="ICommandPublisher{T}" />
    /// 
    public class ServiceBusQueueCommandPublisher<T> : ICommandPublisher<T>, IDisposable where T : CommandBase
    {
        /// <summary>The configuration to use for command publication.</summary>
        private readonly ServiceBusQueueCommandPublisherConfiguration<T> configuration;

        /// <summary>The client to use for operations against the Azure Service Bus queue</summary>
        private readonly Lazy<QueueClient> queueClient;

        /// <summary>The serializer to use for queue messages.</summary>
        private readonly JsonSerializer serializer;

        /// <summary>
        ///   The instance of the logger to use for any telemetry.
        /// </summary>
        /// 
        private ILogger Log { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ServiceBusQueueCommandPublisher{T}" /> class.
        /// </summary>
        /// 
        /// <param name="logger">The logger to use for any telemetry.</param>
        /// <param name="configuration">The configuration to use for command publication.</param>
        ///
        public ServiceBusQueueCommandPublisher(ILogger                                         logger, 
                                               ServiceBusQueueCommandPublisherConfiguration<T> configuration)
        {
            this.Log           = logger        ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            this.queueClient = new Lazy<QueueClient>(() => this.CreateQueueClient(this.configuration), LazyThreadSafetyMode.PublicationOnly);

            this.serializer = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            this.serializer.Converters.Add(new StringEnumConverter());
        }

        /// <summary>
        ///   Publishes the specified command.
        /// </summary>
        /// 
        /// <param name="command">The command to publish.</param>
        /// <param name="publishTimeUtc">If provided, this value will defer publishing of the command until the specified UTC date/time.</param>
        /// 
        public async Task PublishAsync(T        command,
                                       Instant? publishTimeUtc = null)
        {
            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                this.serializer.Serialize(jsonWriter, command);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = command.CorrelationId ?? Guid.NewGuid().ToString();
                    message.MessageId     = command.Id.ToString();
                                        
                    await this.SendMessageAsync(message, publishTimeUtc);
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };
        }

        /// <summary>
        ///   Attempts to publish the specified command.
        /// </summary>
        /// 
        /// <param name="command">The command to publish.</param>
        /// <param name="publishTimeUtc">If provided, this value will defer publishing of the command until the specified UTC date/time.</param>
        /// 
        /// <returns><c>true</c> if the command was successfully published; otherwise, <c>false</c>.</returns>
        /// 
        /// <remarks>
        ///   This implementation will log any failures.
        /// </remarks>
        /// 
        public async Task<bool> TryPublishAsync(T        command,
                                                Instant? publishTimeUtc = null)
        {
            try
            {
                await this.PublishAsync(command, publishTimeUtc);
            }

            catch (Exception ex)
            {
                this.Log
                     .WithCorrelationId(command.CorrelationId)
                     .Error(ex, "Unable to send the {CommandName} command to the command queue to trigger order processing", typeof(T).Name);

                return false;
            }

            return true;
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// 
        public void Dispose()
        {
            if ((this.queueClient.IsValueCreated) && (!this.queueClient.Value.IsClosed))
            {
                this.queueClient.Value.Close();
            }
        }

        /// <summary>
        ///   Creates a client that can be used for publishing to the queue.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for queue client creation.</param>
        /// 
        /// <returns>The queue client to use for publishing commands.</returns>
        /// 
        protected virtual QueueClient CreateQueueClient(ServiceBusQueueCommandPublisherConfiguration<T> configuration)
        {
            var client = QueueClient.CreateFromConnectionString(configuration.ServiceBusConnectionString, configuration.QueueName);

            client.RetryPolicy = new RetryExponential(
                TimeSpan.FromSeconds(configuration.RetryMinimalBackoffTimeSeconds), 
                TimeSpan.FromSeconds(configuration.RetryMaximumlBackoffTimeSeconds),
                configuration.RetryMaximumAttempts);

            return client;
        }

        /// <summary>
        ///   Sends the message using the queue client.
        /// </summary>
        /// 
        /// <param name="message">The message to send</param>
        /// <param name="publishTimeUtc">If provided, this value will defer publishing of the command until the specified UTC date/time.</param>
        /// 
        protected virtual Task SendMessageAsync(BrokeredMessage message,
                                                Instant?        publishTimeUtc)
        {
            if (publishTimeUtc.HasValue)
            {
                return this.queueClient.Value.ScheduleMessageAsync(message, publishTimeUtc.Value.ToDateTimeOffset());
            }

            return this.queueClient.Value.SendAsync(message);            
        }

    }
}
