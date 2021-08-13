using Core.Configuration.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Core.Configuration
{
    public abstract class DbConfigurationProvider : ConfigurationProvider, IDisposable
    {
        public readonly DbConfigurationSource Source;
        private Timer _timer;
        private readonly TimerCallback _timerCallback = s => ((DbConfigurationProvider)s)?.Load();
        private readonly object _lock = new object();

        protected ConfigurationStorageBase Storage { get; }

        protected DbConfigurationProvider(DbConfigurationSource source, ConfigurationStorageBase storage)
        {
            Source = source;
            Storage = storage;
            storage.InitializeAsync().GetAwaiter().GetResult();
            storage.Event += ReLoad;

            _timer = new Timer(_timerCallback, this, source.ReloadDelay, source.ReloadDelay);
        }

        public void Dispose()
        {
            if (_timer == null) return;
            _timer.Dispose();
            _timer = null;
        }

        private void ReLoad(ConcurrentQueue<Event> events)
        {
            if (events == null || !events.Any())
                return;
            lock (_lock)
            {
                if (!events.Any())
                    return;
                while (events.TryDequeue(out var @event))
                {
                    AddOrUpdateKeyValue(@event);
                    RemoveKeyValue(@event);
                }
                OnReload();
            }
        }

        private void AddOrUpdateKeyValue(Event @event)
        {
            if (!@event.IsAdd) return;

            if (Data.ContainsKey(@event.Key))
            {
                Data[@event.Key] = @event.Value;
            }
            else
            {
                Data.Add(@event.Key, @event.Value);
            }
        }

        private void RemoveKeyValue(Event @event)
        {
            if (!@event.IsDelete)
                return;
            if (@event.Key == null)
                return;
            if (Data.ContainsKey(@event.Key))
                Data.Remove(@event.Key);
        }
    }
}
