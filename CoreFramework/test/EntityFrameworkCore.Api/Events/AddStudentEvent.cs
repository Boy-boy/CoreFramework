using Core.EventBus;

namespace EntityFrameworkCore.Api.Events
{

    [MessageName("AddLocalStudentEvent")]
    [MessageGroup("customer")]
    public class AddLocalStudentEvent : Message
    {
        public string AggregateRootId { get; set; }
    }

    [MessageName("AddStudentEvent")]
    [MessageGroup("customer")]
    public class AddStudentEvent : Message
    {
        public string AggregateRootId { get; set; }
    }
}
