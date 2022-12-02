using System.Threading.Tasks;

namespace Core.Ddd.Domain.Events
{
    public interface IDomainEventBus
    {
        Task PublishAsync<TDomainEvent>(TDomainEvent message)
            where TDomainEvent : class, IDomainEvent;

        Task Enqueue<TDomainEvent>(TDomainEvent @event)
            where TDomainEvent : class, IDomainEvent;
    }
}
