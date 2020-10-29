using Core.EventBus.Abstraction;
using System.Collections.Generic;

namespace Core.Ddd.Domain.Events
{
    public class EntityChangeEvent
    {
        private readonly IEventBus _eventBus;

        public EntityChangeEvent(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void PublishAggregateRootEvents(List<AggregateRootEvent> events)
        {
            foreach (var @event in events)
            {
                _eventBus.Publish(@event);
            }
        }
    }
}
