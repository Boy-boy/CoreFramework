using System;
using Core.Ddd.Domain.Events;

namespace Core.EntityFrameworkCore
{
    public class Event
    {
        public Event() { }
        public Event(AggregateRootEvent aggregateRootEvent)
        {
            Id = Guid.NewGuid().ToString();
            EventType = aggregateRootEvent.GetType().ToString();
            EventData = aggregateRootEvent.ToString();
        }

        public string Id { get; set; }

        public string EventType { get; set; }

        public string EventData { get; set; }
    }
}
