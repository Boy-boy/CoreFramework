using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Configuration.Storage
{
    public abstract class ConfigurationStorageBase : IConfigurationStorage
    {
        public event Action<ConcurrentQueue<Event>> Event;

        public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

        public abstract Task<PageResultDto<ConfigurationMessage>> GetAsync(MessageQueryModel query, CancellationToken cancellationToken = default);

        public abstract Task<List<ConfigurationMessage>> GetAsync(string environment, CancellationToken cancellationToken = default);

        public abstract Task<ConfigurationMessage> GetAsync(int id, CancellationToken cancellationToken = default);

        public abstract Task<bool> ExistAsync(string key, CancellationToken cancellationToken);

        public abstract Task<int> GetCountAsync(CancellationToken cancellationToken);


        public abstract Task<int> AddAsync(CreateMessageModel message, CancellationToken cancellationToken = default);

        public abstract Task<int> UpdateAsync(ModifyMessageModel message, CancellationToken cancellationToken = default);

        public abstract Task<int> DeletedAsync(int id, CancellationToken cancellationToken = default);

        protected void InvokeEvent(ConcurrentQueue<Event> events)
        {
            if (events == null || !events.Any())
                return;
            Event?.Invoke(events);
        }
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

        public bool IsDelete => EventType == EventType.Deleted;

        public bool IsAdd => EventType == EventType.Add;

    }

    public enum EventType
    {
        Add = 1,
        Deleted = 3
    }
}
