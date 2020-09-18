using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.RabbitMQ
{
    internal class DefaultRabbitMqMessageConsumer : IRabbitMqMessageConsumer
    {
        private readonly ILogger<DefaultRabbitMqMessageConsumer> _logger;
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private Timer _timer;

        protected ConcurrentBag<Func<IModel, BasicDeliverEventArgs, Task>> ProcessEvents { get; }
        protected RabbitMqExchangeDeclareConfigure ExchangeDeclare { get; private set; }
        protected RabbitMqQueueDeclareConfigure QueueDeclare { get; private set; }
        protected IModel ConsumerChannel { get; private set; }

        protected ConcurrentDictionary<string, string> BindingQueueRoutingKeys { get; }

        public DefaultRabbitMqMessageConsumer(
            IRabbitMqPersistentConnection connection,
            ILogger<DefaultRabbitMqMessageConsumer> logger)
        {
            _logger = logger;
            _persistentConnection = connection;
            ProcessEvents = new ConcurrentBag<Func<IModel, BasicDeliverEventArgs, Task>>();
            BindingQueueRoutingKeys = new ConcurrentDictionary<string, string>();
        }

        public void Initialize(
            RabbitMqExchangeDeclareConfigure exchangeDeclare,
            RabbitMqQueueDeclareConfigure queueDeclare)
        {
            ExchangeDeclare = exchangeDeclare;
            QueueDeclare = queueDeclare;
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _timer = new Timer(sender =>
            {
                TimerCallback();
            }, this, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10));
        }

        public void OnMessageReceived(Func<IModel, BasicDeliverEventArgs, Task> processEvent)
        {
            ProcessEvents.Add(processEvent);
        }

        public Task BindAsync(string routingKey)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            using (var channel = _persistentConnection.CreateModel())
            {
                ExchangeDeclare.Declare(channel);
                QueueDeclare.Declare(channel);
                channel.QueueBind(queue: QueueDeclare.QueueName,
                    exchange: ExchangeDeclare.ExchangeName,
                    routingKey: routingKey);
                BindingQueueRoutingKeys.TryAdd(routingKey, QueueDeclare.QueueName);
            }
            return Task.CompletedTask;
        }

        public Task UnbindAsync(string routingKey)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: QueueDeclare.QueueName,
                    exchange: ExchangeDeclare.ExchangeName,
                    routingKey: routingKey);
                BindingQueueRoutingKeys.TryRemove(routingKey, out _);
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            ConsumerChannel?.Dispose();
            ConsumerChannel = null;
            if (_timer == null)
                return;
            _timer.Dispose();
            _timer = null;
        }

        private void TimerCallback()
        {
            if (ConsumerChannel != null && !ConsumerChannel.IsClosed) return;
            TryCreateConsumerChannel();
            StartBasicConsume();
        }

        private void TryCreateConsumerChannel()
        {
            if (ConsumerChannel != null && !ConsumerChannel.IsClosed) return;
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            ConsumerChannel = _persistentConnection.CreateModel();
        }

        private void StartBasicConsume()
        {
            var consumer = new AsyncEventingBasicConsumer(ConsumerChannel);
            consumer.Received += Consumer_Received;
            ConsumerChannel.BasicQos(0, 50, false);
            ConsumerChannel.BasicConsume(
                queue: QueueDeclare.QueueName,
                autoAck: false,
                consumer: consumer);
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var asyncEventingBasicConsumer = sender as AsyncEventingBasicConsumer;
            try
            {
                foreach (var processEvent in ProcessEvents)
                {
                    await processEvent(asyncEventingBasicConsumer?.Model, eventArgs);
                }
                // Even on exception we take the message off the queue.
                // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
                // For more information see: https://www.rabbitmq.com/dlx.html
                asyncEventingBasicConsumer?.Model?.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public bool HasRoutingKeyBindingQueue()
        {
            return BindingQueueRoutingKeys.Any();
        }
    }
}
