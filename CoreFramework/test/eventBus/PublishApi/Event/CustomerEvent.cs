using Core.EventBus;

namespace PublishApi.Event
{
    [MessageName("customer")]
    public class CustomerEvent:Message
    {
    }
}
