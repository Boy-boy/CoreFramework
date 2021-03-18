using Core.RabbitMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.EventBus.Messaging;

namespace Core.EventBus.RabbitMQ
{
    public class RabbitMqMessageSubscribe : MessageSubscribeBase
    {
        private readonly IMessageHandlerManager _messageHandlerManager;
        private readonly IMessageHandlerProvider _messageHandlerProvider;
        private readonly IRabbitMqMessageConsumerFactory _rabbitMqMessageConsumerFactory;
        private readonly IOptions<EventBusRabbitMqOptions> _options;
        private readonly ILogger<RabbitMqMessageSubscribe> _logger;
        protected ConcurrentDictionary<string, IRabbitMqMessageConsumer> RabbitMqMessageConsumerDic { get; }
        private readonly object _lock = new object();

        public RabbitMqMessageSubscribe(
            IMessageHandlerManager messageHandlerManager,
            IMessageHandlerProvider messageHandlerProvider,
            IRabbitMqMessageConsumerFactory rabbitMqMessageConsumerFactory,
            IOptions<EventBusRabbitMqOptions> options,
            ILogger<RabbitMqMessageSubscribe> logger)
        {
            _messageHandlerManager = messageHandlerManager;
            _messageHandlerProvider = messageHandlerProvider;
            _rabbitMqMessageConsumerFactory = rabbitMqMessageConsumerFactory;
            _options = options;
            _logger = logger;
            RabbitMqMessageConsumerDic = new ConcurrentDictionary<string, IRabbitMqMessageConsumer>();
            messageHandlerManager.OnEventRemoved += SubsManager_OnEventRemoved;
        }

        private void SubsManager_OnEventRemoved(object sender, Type messageType)
        {
            var eventName = MessageNameAttribute.GetNameOrDefault(messageType);
            var (exchangeName, queueName) = GetExchangeNameAndQueueName(messageType);
            var key = $"{exchangeName}_{queueName}";
            lock (_lock)
            {
                if (!RabbitMqMessageConsumerDic.ContainsKey(key)) return;
                RabbitMqMessageConsumerDic.TryGetValue(key, out var rabbitMqMessageConsumer);
                rabbitMqMessageConsumer?.UnbindAsync(eventName);
                if (rabbitMqMessageConsumer != null && rabbitMqMessageConsumer.HasRoutingKeyBindingQueue()) return;
                rabbitMqMessageConsumer?.Dispose();
                RabbitMqMessageConsumerDic.TryRemove(key, out _);
            }
        }

        protected override void Subscribe(Type messageType, Type handlerType)
        {
            TeyCreateMessageConsumer(messageType);
            _messageHandlerManager.AddHandler(messageType, handlerType);
        }

        public override void Subscribe<T, TH>()
        {
            Subscribe(typeof(T), typeof(TH));
        }

        public override void UnSubscribe<T, TH>()
        {
            _messageHandlerManager.RemoveHandler(typeof(T), typeof(TH));
        }

        private void TeyCreateMessageConsumer(Type eventType)
        {
            var (exchangeName, queueName) = GetExchangeNameAndQueueName(eventType);
            var key = $"{exchangeName}_{queueName}";
            lock (_lock)
            {
                if (!RabbitMqMessageConsumerDic.ContainsKey(key))
                {
                    var rabbitMqMessageConsumer = _rabbitMqMessageConsumerFactory.Create(
                        new RabbitMqExchangeDeclareConfigure(exchangeName, "direct", true),
                        new RabbitMqQueueDeclareConfigure(queueName));
                    rabbitMqMessageConsumer.OnMessageReceived(Consumer_Received);
                    RabbitMqMessageConsumerDic.TryAdd(key, rabbitMqMessageConsumer);
                }
                RabbitMqMessageConsumerDic.TryGetValue(key, out var rabbitMqMessageConsumer1);
                var eventName = MessageNameAttribute.GetNameOrDefault(eventType);
                rabbitMqMessageConsumer1?.BindAsync(eventName);
            }
        }

        private (string ExchangeName, string QueueName) GetExchangeNameAndQueueName(Type eventType)
        {
            var subscribeConfigure = _options.Value.RabbitSubscribeConfigures.LastOrDefault(p => p.EventType == eventType);
            if (subscribeConfigure == null)
                return (RabbitMqConst.DefaultExchangeName, RabbitMqConst.DefaultQueueName);

            var (exchangeName, queueName) = subscribeConfigure.GetExchangeNameAndQueueName(eventType);
            exchangeName = exchangeName ?? RabbitMqConst.DefaultExchangeName;
            queueName = queueName ?? RabbitMqConst.DefaultQueueName;
            return (exchangeName, queueName);
        }

        private async Task Consumer_Received(IModel model, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
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

            if (_messageHandlerManager.MessageTypeMappingDict.TryGetValue(eventName, out var messageType))
            {
                var integrationEvent = (IMessage)JsonConvert.DeserializeObject(message, messageType);
                var messageHandlers = _messageHandlerProvider.GetHandlers(messageType);
                foreach (var messageHandler in messageHandlers)
                {
                    var concreteType = typeof(IMessageHandler<>).MakeGenericType(messageType);
                    var method = concreteType.GetMethod("HandAsync");
                    if (method != null)
                    {
                        await (Task)method.Invoke(messageHandler, new object[] { integrationEvent });
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
