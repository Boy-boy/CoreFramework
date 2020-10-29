using System;
using Core.Ddd.Domain.Events;

namespace Core.EntityFrameworkCore
{
    public class Event
    {
        public Event(AggregateRootEvent aggregateRootEvent)
        {
            Id = Guid.NewGuid().ToString();
            EventType = aggregateRootEvent.GetType();
            EventData = aggregateRootEvent;
        }

        public string Id { get; set; }

        public Type EventType { get; set; }

        public object EventData { get; set; }
    }
}
