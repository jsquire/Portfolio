using System;
using System.Linq;

namespace OrderFulfillment.Core.Configuration
{
    /// <summary>
    ///    The set of configuration neeed to allows the <see cref="ServiceBusQueueCommandPublisher{T}" />
    ///    to publish commands.
    /// </summary>
    /// 
    /// <typeparam name="T">The type of command the configuration applies to.</typeparam>
    /// 
    public class ServiceBusQueueCommandPublisherConfiguration<T> : IConfiguration
    {
        /// <summary>
        ///   The connection string to the service bus namespace of the command
        ///   queue.
        /// </summary>
        /// 
        public string ServiceBusConnectionString { get;  set; }

        /// <summary>
        ///   The name of the command queue to use for publishing.
        /// </summary>
        /// 
        public string QueueName { get;  set; }

        /// <summary>
        ///   The maximum number of attempts to retry sending a command to the bus.
        /// </summary>
        /// 
        public int RetryMaximumAttempts { get;  set; }

        /// <summary>
        ///   The mimimum interval for backing off.  This is added to the retry interval
        ///   computed from the delta. 
        /// </summary>
        /// 
        public int RetryMinimalBackoffTimeSeconds { get;  set; }

        /// <summary>
        ///   The maximum interval for backing off.  If the computed backoff exceeds
        ///   this interval, the maximum will be used instead.
        /// </summary>
        /// 
        public int RetryMaximumlBackoffTimeSeconds { get;  set; }

        /// <summary>
        ///   Gets the type for a specific, non-generic instance of the
        ///   assciated configuration.
        /// </summary>
        /// 
        /// <param name="genericType">The closed generic type of ServiceBusQueueCommandPublisherConfiguration</param>
        /// 
        /// <returns>A type that represents the specific associated configuration type for the closed generic.</returns>
        /// 
        public static Type GetSpecificConfigurationType(Type genericType)
        {
            var sourceAssembly = typeof(ServiceBusQueueCommandPublisherConfiguration<T>).Assembly;
            var name           = genericType.Name;
            var genericMarker  = name.IndexOf('`');
            var nameNoGeneric  = (genericMarker != -1) ? name.Substring(0, genericMarker) : name;
            var configName     = $"{ genericType.GetGenericArguments().FirstOrDefault()?.Name }{ nameNoGeneric }";
            
            return sourceAssembly.GetTypes().SingleOrDefault(currentType => currentType.Name == configName);
        }
    }
}
