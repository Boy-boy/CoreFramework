using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Configuration.Storage
{
    public abstract class ConfigurationStorageBase : IConfigurationStorage
    {
        public static ConcurrentQueue<Event> Events = new ConcurrentQueue<Event>();

        public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

        public abstract Task<ConfigurationMessage> GetAsync(string id, CancellationToken cancellationToken = default);

        public abstract Task<List<ConfigurationMessage>> GetAsync(CancellationToken cancellationToken = default);

        public abstract Task AddAsync(ConfigurationMessage message, CancellationToken cancellationToken = default);

        public abstract Task UpdateAsync(ConfigurationMessage message, CancellationToken cancellationToken = default);

        public abstract Task DeletedAsync(string id, CancellationToken cancellationToken = default);
    }

    public class Event
    {
        public Event(EventType eventType, string key, string value)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key)); ;
            Value = value;
            EventType = eventType;
        }
        public EventType EventType { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }
    }

    public enum EventType
    {
        Add = 1,
        Update = 2,
        Deleted = 3
    }
}
