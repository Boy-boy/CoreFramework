using Core.Ddd.Domain.Events;
using Core.EventBus;

namespace EntityFrameworkCore.Api.Events
{
    [MessageName("customer")]
    public class AddStudentEvent: AggregateRootEvent
    {
        public string AggregateRootId{ get; set; }
    }
}
