using Core.Configuration.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;

namespace Core.Configuration
{
    public abstract class DbConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private event Action<Event> Event;
        private Timer _timer;
        private Timer _timer1;
        private readonly object _lock = new { };
        private bool _activatedReLoad;
        private readonly TimerCallback _timerCallback = s => ((DbConfigurationProvider)s)?.Load();
        private readonly TimerCallback _timerCallback1 = s => ((DbConfigurationProvider)s)?.ReLoad();

        protected ConfigurationStorageBase Storage { get; }

        protected DbConfigurationProvider(DbConfigurationSource source, ConfigurationStorageBase storage)
        {
            Storage = storage;
            storage.InitializeAsync().GetAwaiter().GetResult();
            Event += AddOrUpdateKeyValue;
            Event += RemoveKeyValue;
            _timer = new Timer(_timerCallback, this, source.ReloadDelay, source.ReloadDelay);
            _timer1 = new Timer(_timerCallback1, this, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void ReLoad()
        {
            if (_activatedReLoad) return;

            var hasEvent = ConfigurationStorageBase.Events.IsEmpty;
            if (hasEvent) return;

            lock (_lock)
            {
                _activatedReLoad = true;

                while (!ConfigurationStorageBase.Events.IsEmpty)
                {
                    if (!ConfigurationStorageBase.Events.TryDequeue(out var @event)) continue;
                    Event?.Invoke(@event);
                }
                OnReload();

                _activatedReLoad = false;
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
            if (_timer1 != null)
            {
                _timer1.Dispose();
                _timer1 = null;
            }
            Event = null;
        }

        private void AddOrUpdateKeyValue(Event @event)
        {
            if (@event.EventType != EventType.Add && @event.EventType != EventType.Update)
                return;
            if (@event.Key == null)
                return;
            if (Data.ContainsKey(@event.Key))
            {
                switch (@event.EventType)
                {
                    case EventType.Add:
                    case EventType.Update:
                        Data[@event.Key] = @event.Value;
                        break;
                    case EventType.Deleted:
                        Data.Remove(@event.Key);
                        break;
                }
            }
            else
            {
                switch (@event.EventType)
                {
                    case EventType.Add:
                    case EventType.Update:
                        Data.Add(@event.Key, @event.Value);
                        break;
                    case EventType.Deleted:
                        break;
                }
            }
        }

        private void RemoveKeyValue(Event @event)
        {
            if (@event.EventType != EventType.Deleted)
                return;
            if (@event.Key == null)
                return;
            if (Data.ContainsKey(@event.Key))
                Data.Remove(@event.Key);
        }
    }
}
