using System.Collections.Generic;
using Core.Ddd.Domain.Events;

namespace Core.Ddd.Domain.Entities
{
    public class AggregateRoot : Entity, IAggregateRoot
    {
        public Queue<AggregateRootEvent> Events { get; }

        public AggregateRoot()
        {
            Events = new Queue<AggregateRootEvent>();
        }

        public void OnEvent(AggregateRootEvent @event)
        {
            Events.Enqueue(@event);
        }
    }
}
