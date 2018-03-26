using OrderFulfillment.Core.Commands;

namespace OrderFulfillment.Core.Configuration
{
    /// <summary>
    ///    The set of configuration neeed to allows the <see cref="ServiceBusQueueCommandPublisher{T}" />
    ///    to publish the <see cref="ProcessOrder" /> command.
    /// </summary>
    /// 
    public class ProcessOrderServiceBusQueueCommandPublisherConfiguration : ServiceBusQueueCommandPublisherConfiguration<ProcessOrder>, IConfiguration
    {
    }
}
