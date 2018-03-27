using OrderFulfillment.Core.Events;

namespace OrderFulfillment.Core.Configuration
{
    // <summary>
    ///    The set of configuration neeed to allows the <see cref="ServiceBusTopicEventPublisher{T}" />
    ///    to publish events from the <see cref="EventBase" /> family.
    /// </summary>
    /// 
    public class EventBaseServiceBusTopicEventPublisherConfiguration : ServiceBusTopicEventPublisherConfiguration<EventBase>, IConfiguration
    {
    }
}
