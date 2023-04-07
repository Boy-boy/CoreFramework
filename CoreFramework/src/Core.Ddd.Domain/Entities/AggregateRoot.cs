using Core.EventBus;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Core.Ddd.Domain.Entities
{
    public class AggregateRoot : Entity, IAggregateRoot
    {
        private readonly ICollection<IMessage> _localEvents = new Collection<IMessage>();

        private readonly ICollection<IMessage> _distributedEvents = new Collection<IMessage>();

        public void AddLocalEvent(Message @event)
        {
            _localEvents.Add(@event);
        }

        public void AddDistributedEvent(Message @event)
        {
            _distributedEvents.Add(@event);
        }

        public IEnumerable<IMessage> GetLocalEvents()
        {
            return _localEvents;
        }

        public IEnumerable<IMessage> GetDistributedEvents()
        {
            return _distributedEvents;
        }

        public void CleanLocalEvents()
        {
            _localEvents.Clear();
        }

        public void CleanDistributedEvents()
        {
            _distributedEvents.Clear();
        }
    }

    public class AggregateRoot<TKey> : AggregateRoot, IAggregateRoot<TKey>
    {
        public TKey Id { get; set; }
    }
}
