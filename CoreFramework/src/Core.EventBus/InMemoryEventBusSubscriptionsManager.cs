using Core.EventBus.Abstraction;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Core.EventBus
{
    public class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
    {
        public event EventHandler<Type> OnEventRemoved;

        private ConcurrentDictionary<string, List<Type>> _handlers { get; }
        private ConcurrentDictionary<string, Type> _eventTypes { get; }

        public InMemoryEventBusSubscriptionsManager()
        {
            _handlers = new ConcurrentDictionary<string, List<Type>>();
            _eventTypes = new ConcurrentDictionary<string, Type>();
        }

        #region AddSubscription
        public void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>, new()
        {
            AddSubscription(typeof(T), typeof(TH));
        }
        public void AddSubscription(Type eventType, Type handlerType)
        {
            if (!typeof(IIntegrationEventHandler<>).MakeGenericType(eventType).IsAssignableFrom(handlerType)) return;
            TryAddSubscriptionEventType(eventType);
            TryAddSubscriptionEventHandler(eventType, handlerType);
        }
        private void TryAddSubscriptionEventType(Type eventType)
        {
            var eventName = EventNameAttribute.GetNameOrDefault(eventType);
            if (_eventTypes.ContainsKey(eventName))
            {
                //duplicate event key
                if (_eventTypes[eventName] != eventType)
                {
                    throw new ArgumentException(
                        $"Event name {eventName} already exists,please make sure the event key is unique");
                }
            }
            else
            {
                _eventTypes[eventName] = eventType;
            }
        }
        private void TryAddSubscriptionEventHandler(Type eventType, Type handlerType)
        {
            var eventName = EventNameAttribute.GetNameOrDefault(eventType);
            if (!IncludeSubscriptionsHandlesForEventName(eventName))
            {
                _handlers.GetOrAdd(eventName, new List<Type>());
            }
            if (_handlers[eventName].Any(s => s == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
            }
            _handlers[eventName].Add(handlerType);
        }
        #endregion

        #region RemoveSubscription
        public void RemoveSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>, new()
        {
            RemoveSubscription(typeof(T), typeof(TH));
        }
        public void RemoveSubscription(Type eventType, Type handlerType)
        {
            if (!typeof(IIntegrationEventHandler<>).MakeGenericType(eventType).IsAssignableFrom(handlerType)) return;
            var eventName = EventNameAttribute.GetNameOrDefault(eventType);
            var handlerToRemove = TryFindSubscriptionToRemove(eventName, handlerType);
            TryRemoveHandler(eventName, handlerToRemove);

        }
        private Type TryFindSubscriptionToRemove(string eventName, Type handlerType)
        {
            return !IncludeSubscriptionsHandlesForEventName(eventName) ? null : _handlers[eventName].SingleOrDefault(s => s == handlerType);
        }
        private void TryRemoveHandler(string eventName, Type subsToRemove)
        {
            if (subsToRemove == null) return;
            _handlers[eventName].Remove(subsToRemove);
            if (_handlers[eventName].Any()) return;
            _handlers.TryRemove(eventName, out _);
            RaiseOnEventRemoved(eventName);
        }
        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            _eventTypes.TryGetValue(eventName, out var eventType);
            handler?.Invoke(this, eventType);
            if (eventType != null)
            {
                _eventTypes.TryRemove(eventName, out _);
            }
        }
        #endregion

        public bool IncludeSubscriptionsHandlesForEventName(string eventName) => _handlers.ContainsKey(eventName);

        public bool IncludeEventTypeForEventName(string eventName) => _eventTypes.ContainsKey(eventName);

        public List<Type> TryGetEventHandlerTypes(string eventName)
        {
            _handlers.TryGetValue(eventName, out var handles);
            return handles ?? new List<Type>();
        }

        public Type TryGetEventTypeForEventName(string eventName)
        {
            _eventTypes.TryGetValue(eventName, out var eventType);
            return eventType;
        }

        public void Dispose()
        {
            _handlers?.Clear();
            _eventTypes?.Clear();
        }
    }
}
