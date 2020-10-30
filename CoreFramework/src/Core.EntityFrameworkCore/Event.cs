using System;
using Core.Ddd.Domain.Events;
using Core.Json.Newtonsoft;

namespace Core.EntityFrameworkCore
{
    public class Event
    {
        public Event() { }
        public Event(AggregateRootEvent aggregateRootEvent)
        {
            Id = Guid.NewGuid().ToString();
            EventType = aggregateRootEvent.GetType().ToString();
            EventData = NewtonsoftJsonSerializer.Serialize(aggregateRootEvent);
            CreateTime = DateTime.Now;
        }

        public string Id { get; set; }

        public string EventType { get; set; }

        public string EventData { get; set; }

        public DateTime CreateTime { get; set; }
    }
}
