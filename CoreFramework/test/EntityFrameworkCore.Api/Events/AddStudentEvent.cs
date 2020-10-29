using Core.Ddd.Domain.Events;

namespace EntityFrameworkCore.Api.Events
{
    public class AddStudentEvent: AggregateRootEvent
    {
        public string AggregateRootId{ get; set; }
    }
}
