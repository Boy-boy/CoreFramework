using Core.EventBus;

namespace SubscriptionApi.Event
{
    [MessageName("customer")]
    public class CustomerEvent : Message
    {
    }
}
