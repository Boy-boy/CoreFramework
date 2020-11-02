using Core.EventBus.Abstraction;
using Core.RabbitMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Core.EventBus.RabbitMQ
{
    public class EventBusRabbitMq : EventBusBase, IDisposable
    {
        const string EXCHANGE_NAME = "event_bus_rabbitmq_default_exchange";
        const string QUEUE_NAME = "event_bus_rabbitmq_default_queue";
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly IRabbitMqMessageConsumerFactory _rabbitMqMessageConsumerFactory;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly IEventHandlerFactory _eventHandlerFactory;
        private readonly ILogger<EventBusRabbitMq> _logger;
        private readonly EventBusRabbitMqOptions _eventBusRabbitMqOptions;
        private readonly int _retryCount = 2;
        private readonly object _lock = new object();
        protected ConcurrentDictionary<string, IRabbitMqMessageConsumer> RabbitMqMessageConsumerDic { get; private set; }


        public EventBusRabbitMq(
            IRabbitMqPersistentConnection persistentConnection,
            IRabbitMqMessageConsumerFactory rabbitMqMessageConsumerFactory,
            IEventBusSubscriptionsManager subsManager,
            IEventHandlerFactory eventHandlerFactory,
            ILogger<EventBusRabbitMq> logger,
            IOptions<EventBusRabbitMqOptions> options)
        {
            _eventBusRabbitMqOptions = options.Value;
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _rabbitMqMessageConsumerFactory = rabbitMqMessageConsumerFactory ?? throw new ArgumentNullException(nameof(subsManager));
            _subsManager = subsManager ?? throw new ArgumentNullException(nameof(subsManager));
            _eventHandlerFactory = eventHandlerFactory ?? throw new ArgumentNullException(nameof(eventHandlerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            RabbitMqMessageConsumerDic = new ConcurrentDictionary<string, IRabbitMqMessageConsumer>();
            _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
        }

        private void SubsManager_OnEventRemoved(object sender, Type eventType)
        {
            var eventName = EventNameAttribute.GetNameOrDefault(eventType);
            var (exchangeName, queueName) = GetExchangeNameAndQueueName(eventType);
            var key = $"{exchangeName}_{queueName}";
            if (!RabbitMqMessageConsumerDic.ContainsKey(key)) return;
            var rabbitMqMessageConsumer = RabbitMqMessageConsumerDic[key];
            rabbitMqMessageConsumer.UnbindAsync(eventName);
            if (rabbitMqMessageConsumer.HasRoutingKeyBindingQueue()) return;
            rabbitMqMessageConsumer.Dispose();
            RabbitMqMessageConsumerDic.TryRemove(key, out _);
        }

        protected override void Publish(Type eventType, IntegrationEvent eventDate)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", eventDate.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

            var eventName = EventNameAttribute.GetNameOrDefault(eventType);
            _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", eventDate.Id, eventName);

            using (var channel = _persistentConnection.CreateModel())
            {
                _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", eventDate.Id);
                var message = JsonConvert.SerializeObject(eventDate);
                var body = Encoding.UTF8.GetBytes(message);

                var model = channel;
                var exchangeName = _eventBusRabbitMqOptions.RabbitMqPublishConfigure.GetExchangeName() ?? EXCHANGE_NAME;
                model.ExchangeDeclare(exchange: exchangeName, type: "direct", durable: true);
                policy.Execute(() =>
                {
                    var properties = model.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent

                    _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", eventDate.Id);

                    model.BasicPublish(
                        exchange: exchangeName,
                        routingKey: eventName,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);
                });
            }
        }

        protected override void Subscribe(Type eventType, Type handlerType)
        {
            TeyCreateMessageConsumer(eventType);
            _subsManager.AddSubscription(eventType, handlerType);

        }

        protected override void UnSubscribe(Type eventType, Type handlerType)
        {
            var eventName = EventNameAttribute.GetNameOrDefault(eventType);
            _logger.LogInformation("Unsubscribing from event {EventName}", eventName);
            _subsManager.RemoveSubscription(eventType, handlerType);
        }

        public void Dispose()
        {
            foreach (var rabbitMqMessageConsumer in RabbitMqMessageConsumerDic)
            {
                rabbitMqMessageConsumer.Value?.Dispose();
            }
            RabbitMqMessageConsumerDic = new ConcurrentDictionary<string, IRabbitMqMessageConsumer>();
        }

        private void TeyCreateMessageConsumer(Type eventType)
        {
            var (exchangeName, queueName) = GetExchangeNameAndQueueName(eventType);
            var key = $"{exchangeName}_{queueName}";
            if (RabbitMqMessageConsumerDic.ContainsKey(key))
                return;
            lock (_lock)
            {
                if (RabbitMqMessageConsumerDic.ContainsKey(key))
                    return;
                var rabbitMqMessageConsumer = _rabbitMqMessageConsumerFactory.Create(
                    new RabbitMqExchangeDeclareConfigure(exchangeName, "direct", true),
                    new RabbitMqQueueDeclareConfigure(queueName));
                var eventName = EventNameAttribute.GetNameOrDefault(eventType);
                rabbitMqMessageConsumer.BindAsync(eventName);
                rabbitMqMessageConsumer.OnMessageReceived(Consumer_Received);
                RabbitMqMessageConsumerDic.TryAdd(key, rabbitMqMessageConsumer);
            }
        }

        private (string ExchangeName, string QueueName) GetExchangeNameAndQueueName(Type eventType)
        {
            var subscribeConfigure = _eventBusRabbitMqOptions.RabbitSubscribeConfigures.LastOrDefault(p => p.EventType == eventType);
            if (subscribeConfigure == null)
                return (EXCHANGE_NAME, QUEUE_NAME);

            var (exchangeName, queueName) = subscribeConfigure.GetExchangeNameAndQueueName(eventType);

            exchangeName = exchangeName ?? EXCHANGE_NAME;
            queueName = queueName ?? QUEUE_NAME;

            return (exchangeName, queueName);
        }

        private async Task Consumer_Received(IModel model, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body);
            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }
                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message, "----- ERROR Processing message \"{Message}\"", message);
            }
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            _logger.LogTrace("Processing RabbitMQ event: {eventName}", eventName);
            if (_subsManager.IncludeEventTypeForEventName(eventName))
            {
                var eventHandleTypes = _subsManager.TryGetEventHandlerTypes(eventName);
                foreach (var eventHandleType in eventHandleTypes)
                {
                    var handlerInstance = _eventHandlerFactory.GetHandler(eventHandleType);
                    var eventType = _subsManager.TryGetEventTypeForEventName(eventName);
                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await Task.Yield();
                    var method = concreteType.GetMethod("Handle");
                    if (method != null)
                    {
                        await (Task)method.Invoke(handlerInstance, new[] { integrationEvent });
                    }
                }
            }
            else
            {
                _logger.LogWarning("No subscription for RabbitMQ event: {eventName}", eventName);
            }

        }
    }
}
