using Core.EventBus;

namespace EntityFrameworkCore.Api.Events
{
    [MessageName("customer")]
    public class AddStudentEvent : Message
    {
        public string AggregateRootId { get; set; }
    }
}
