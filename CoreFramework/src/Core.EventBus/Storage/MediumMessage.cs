using Core.Json.Newtonsoft;
using System;

namespace Core.EventBus.Storage
{
    public class MediumMessage
    {
        public MediumMessage()
        {
        }

        public MediumMessage(IMessage aggregateRootEvent)
        {
            Id = Guid.NewGuid();
            Version = 1;
            AssemblyName = aggregateRootEvent.GetType().Assembly.GetName().Name;
            MessageName = aggregateRootEvent.GetType().FullName;
            MessageData = aggregateRootEvent.ToJson();
            CreateTime = DateTime.Now;
            UtcTime = DateTime.UtcNow;
        }

        public Guid Id { get; set; }

        public int Version { get; set; }

        public string AssemblyName { get; set; }

        public string MessageName { get; set; }

        public string MessageData { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UtcTime { get; set; }
    }
}
