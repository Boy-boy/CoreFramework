using Core.EventBus;

namespace PublishApi.Event
{
    [EventName("customer")]
    public class CustomerEvent:IntegrationEvent
    {
    }
}
