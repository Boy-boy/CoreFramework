using System.Collections.Generic;
using System.Collections.ObjectModel;
using Core.Ddd.Domain.Events;

namespace Core.Ddd.Domain.Entities
{
    public class AggregateRoot : Entity, IAggregateRoot
    {
        private readonly ICollection<AggregateRootEvent> _events = new Collection<AggregateRootEvent>();

        public void AddEvent(AggregateRootEvent @event)
        {
            _events.Add(@event);
        }

        public IEnumerable<AggregateRootEvent> GetEvents()
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
