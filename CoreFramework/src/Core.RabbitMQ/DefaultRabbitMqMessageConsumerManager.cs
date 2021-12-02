using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Core.RabbitMQ
{
    public class DefaultRabbitMqMessageConsumerManager : IRabbitMqMessageConsumerManager
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly ConcurrentDictionary<string, IRabbitMqMessageConsumer> _consumers;

        private readonly object _lock = new();

        public DefaultRabbitMqMessageConsumerManager(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _consumers = new ConcurrentDictionary<string, IRabbitMqMessageConsumer>();
        }

        public IRabbitMqMessageConsumer TryCreate(RabbitMqExchangeDeclareConfigure exchangeDeclare,
            RabbitMqQueueDeclareConfigure queueDeclare)
        {
            if (exchangeDeclare == null)
                throw new ArgumentNullException(nameof(exchangeDeclare));

            if (queueDeclare == null)
                throw new ArgumentNullException(nameof(queueDeclare));

            lock (_lock)
            {
                var key = $"{exchangeDeclare.ExchangeName}_{queueDeclare.QueueName}";
                if (_consumers.TryGetValue(key, out var consumer))
                    return consumer;

                consumer = Create(exchangeDeclare, queueDeclare);
                _consumers.TryAdd(key, consumer);
                return consumer;
            }
        }

        public bool TryGet(string exchangeName, string queueName, out IRabbitMqMessageConsumer consumer)
        {
            lock (_lock)
            {
                var key = $"{exchangeName}_{queueName}";
                return _consumers.TryGetValue(key, out consumer);
            }
        }

        public bool TryRemove(string exchangeName, string queueName)
        {
            lock (_lock)
            {
                var key = $"{exchangeName}_{queueName}";
                return _consumers.TryRemove(key, out _);
            }
        }

        private IRabbitMqMessageConsumer Create(RabbitMqExchangeDeclareConfigure exchangeDeclare,
            RabbitMqQueueDeclareConfigure queueDeclare)
        {
            var consumer = (DefaultRabbitMqMessageConsumer)ActivatorUtilities.CreateInstance(_serviceScopeFactory.CreateScope().ServiceProvider,
                typeof(DefaultRabbitMqMessageConsumer));
            consumer.Initialize(exchangeDeclare, queueDeclare);
            return consumer;
        }
    }
}
