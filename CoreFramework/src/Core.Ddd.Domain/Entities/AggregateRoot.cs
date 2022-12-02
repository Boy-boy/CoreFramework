using System.Collections.Generic;
using System.Collections.ObjectModel;
using Core.Ddd.Domain.Events;

namespace Core.Ddd.Domain.Entities
{
    public class AggregateRoot : Entity, IAggregateRoot
    {
        private readonly ICollection<IDomainEvent> _events = new Collection<IDomainEvent>();

        public void AddEvent(IDomainEvent @event)
        {
            _events.Add(@event);
        }

        public IEnumerable<IDomainEvent> GetEvents()
        {
            return _events;
        }

        public void CleanEvents()
        {
            _events.Clear();
        }
    }

    public class AggregateRoot<TKey> : AggregateRoot, IAggregateRoot<TKey>
    {
        public TKey Id { get; set; }
    }
}
