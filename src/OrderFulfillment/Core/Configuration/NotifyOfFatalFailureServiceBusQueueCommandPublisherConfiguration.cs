using OrderFulfillment.Core.Commands;

namespace OrderFulfillment.Core.Configuration
{
    /// <summary>
    ///    The set of configuration neeed to allows the <see cref="ServiceBusQueueCommandPublisher{T}" />
    ///    to publish the <see cref="NotifyOfFatalFailure" /> command.
    /// </summary>
    /// 
    public class NotifyOfFatalFailureServiceBusQueueCommandPublisherConfiguration : ServiceBusQueueCommandPublisherConfiguration<NotifyOfFatalFailure>, IConfiguration
    {
    }
}
