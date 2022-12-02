using Core.Ddd.Domain.Events;
using Core.EventBus;

namespace EntityFrameworkCore.Api.Events
{
    [MessageName("customer")]
    public class AddStudentEvent : DomainEvent
    {
        public string AggregateRootId { get; set; }
    }
}
