using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Core.EventBus.Integration;
using Core.EventBus.Local;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Ddd.Domain.Events
{
    public class DomainEventBus : IDomainEventBus
    {
        private readonly ConcurrentQueue<IDomainEvent> _eventQueue = new();
        private readonly ILocalMessagePublisher _localMessagePublisher;
        private readonly IIntegrationMessagePublisher _integrationMessagePublisher;

        public DomainEventBus(IServiceProvider serviceProvider)
        {
            _localMessagePublisher = serviceProvider.GetService<ILocalMessagePublisher>();
            _integrationMessagePublisher = serviceProvider.GetService<IIntegrationMessagePublisher>();
        }

        public async Task PublishAsync<TDomainEvent>(TDomainEvent message)
            where TDomainEvent : class, IDomainEvent
        {
            if (message is IIntegrationDomainEvent integrationDomainEvent)
            {
                if (_integrationMessagePublisher == null)
                {
                    throw new Exception(nameof(_integrationMessagePublisher));
                }
                await _integrationMessagePublisher.PublishAsync(integrationDomainEvent);
            }
            else
            {
                if (_localMessagePublisher == null)
                {
                    throw new Exception(nameof(_localMessagePublisher));
                }
                await _localMessagePublisher.PublishAsync(message);
            }
        }

        public async Task PublishQueueAsync()
        {
            while (_eventQueue.TryDequeue(out var @event))
            {
                await PublishAsync(@event);
            }
        }

        public Task Enqueue<TDomainEvent>(TDomainEvent @event)
            where TDomainEvent : class, IDomainEvent
        {
            _eventQueue.Enqueue(@event);
            return Task.CompletedTask;
        }
    }
}
