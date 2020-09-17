using Core.EventBus;

namespace SubscriptionApi.Event
{
    [EventName("customer")]
    public class CustomerEvent : IntegrationEvent
    {
    }
}
