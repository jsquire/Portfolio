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
using Serilog;

namespace OrderFulfillment.Core.Events
{
    /// <summary>
    ///   Serves as a publsiher for a single event, or family of events with 
    ///   a common ancestor using Azure Servuce Bus topics as an underlying storage
    ///   mechnaism.
    /// </summary>
    /// 
    /// <typeparam name="T">The type of event capable of being published</typeparam>
    /// 
    /// <seealso cref="IEventPublisher{T}" />
    /// 
    public class ServiceBusTopicEventPublisher<T> : IEventPublisher<T>, IDisposable where T : EventBase
    {
        /// <summary>The configuration to use for event publication.</summary>
        private readonly ServiceBusTopicEventPublisherConfiguration<T> configuration;

        /// <summary>The client to use for operations against the Azure Service Bus topic</summary>
        private readonly Lazy<TopicClient> topicClient;

        /// <summary>The serializer to use for topic messages.</summary>
        private readonly JsonSerializer serializer;

        /// <summary>
        ///   The instance of the logger to use for any telemetry.
        /// </summary>
        /// 
        private ILogger Log { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ServiceBusTopicEventdPublisher{T}" /> class.
        /// </summary>
        /// 
        /// <param name="logger">The logger to use for any telemetry.</param>
        /// <param name="configuration">The configuration to use for event publication.</param>
        ///
        public ServiceBusTopicEventPublisher(ILogger                                       logger, 
                                             ServiceBusTopicEventPublisherConfiguration<T> configuration)
        {
            this.Log           = logger        ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            this.topicClient = new Lazy<TopicClient>(() => this.CreateTopicClient(this.configuration), LazyThreadSafetyMode.PublicationOnly);

            this.serializer = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            this.serializer.Converters.Add(new StringEnumConverter());
        }

        /// <summary>
        ///   Publishes the specified event.
        /// </summary>
        /// 
        /// <param name="event">The event to publish.</param>
        /// 
        public async Task PublishAsync(T @event)
        {
            using (var memStream  = new MemoryStream())
            using (var writer     = new StreamWriter(memStream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {                
                this.serializer.Serialize(jsonWriter, @event);
                jsonWriter.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (var message = new BrokeredMessage(memStream))
                {
                    message.ContentType   = MimeTypes.Json;
                    message.CorrelationId = @event.CorrelationId ?? Guid.NewGuid().ToString();
                    message.MessageId     = @event.Id.ToString();

                    await this.SendMessageAsync(message);
                }

                jsonWriter.Close();
                writer.Close();
                memStream.Close();
            };
        }

        /// <summary>
        ///   Attempts to publish the specified event.
        /// </summary>
        /// 
        /// <param name="event">The event to publish.</param>
        /// 
        /// <returns><c>true</c> if the event was successfully published; otherwise, <c>false</c>.</returns>
        /// 
        /// <remarks>
        ///   This implementation will log any failures.
        /// </remarks>
        /// 
        public async Task<bool> TryPublishAsync(T @event)
        {
            try
            {
                await this.PublishAsync(@event);
            }

            catch (Exception ex)
            {
                this.Log
                     .WithCorrelationId(@event.CorrelationId)
                     .Error(ex, "Unable to publish the {EventName} event to the event stream. {Event}", typeof(T).Name, @event);

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
            if ((this.topicClient.IsValueCreated) && (!this.topicClient.Value.IsClosed))
            {
                this.topicClient.Value.Close();
            }
        }

        /// <summary>
        ///   Creates a client that can be used for publishing to the topic.
        /// </summary>
        /// 
        /// <param name="configuration">The configuration to use for topic client creation.</param>
        /// 
        /// <returns>The topic client to use for publishing events.</returns>
        /// 
        protected virtual TopicClient CreateTopicClient(ServiceBusTopicEventPublisherConfiguration<T> configuration)
        {
            var client = TopicClient.CreateFromConnectionString(configuration.ServiceBusConnectionString, configuration.TopicName);

            client.RetryPolicy = new RetryExponential(
                TimeSpan.FromSeconds(configuration.RetryMinimalBackoffTimeSeconds), 
                TimeSpan.FromSeconds(configuration.RetryMaximumlBackoffTimeSeconds),
                configuration.RetryMaximumAttempts);

            return client;
        }

        /// <summary>
        ///   Sends the message using the topic client.
        /// </summary>
        /// 
        /// <param name="message">The message to send</param>
        /// 
        protected virtual Task SendMessageAsync(BrokeredMessage message)
        {
            return this.topicClient.Value.SendAsync(message);
        }

    }
}
