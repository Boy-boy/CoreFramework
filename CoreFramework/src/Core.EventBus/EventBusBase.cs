using Core.EventBus.Abstraction;
using System;
using System.Threading.Tasks;

namespace Core.EventBus
{
    public abstract class EventBusBase : IEventBus
    {
        public Task PublishAsync<TEvent>(TEvent eventData)
            where TEvent : IntegrationEvent
        {
            return PublishAsync(typeof(TEvent), eventData);
        }

        protected abstract Task PublishAsync(Type eventType, IntegrationEvent eventDate);

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>, new()
        {
            Subscribe(typeof(T), typeof(TH));
        }

        protected abstract void Subscribe(Type eventType, Type handlerType);

        public void UnSubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>, new()
        {
            UnSubscribe(typeof(T), typeof(TH));
        }

        protected abstract void UnSubscribe(Type eventType, Type handlerType);
    }
}
