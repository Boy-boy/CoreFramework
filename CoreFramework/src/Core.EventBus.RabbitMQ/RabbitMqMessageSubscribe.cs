using Core.EventBus.Messaging;
using Core.EventBus.Messaging.Diagnostics;
using Core.RabbitMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.EventBus.RabbitMQ
{
    public class RabbitMqMessageSubscribe : MessageSubscribeBase
    {
        private readonly IMessageHandlerManager _messageHandlerManager;
        private readonly IMessageHandlerProvider _messageHandlerProvider;
        private readonly IRabbitMqMessageConsumerManager _rabbitMqMessageConsumerManager;
        private readonly IOptions<EventBusRabbitMqOptions> _options;
        private readonly ILogger<RabbitMqMessageSubscribe> _logger;
        private readonly object _lock = new();

        public RabbitMqMessageSubscribe(
            IMessageHandlerManager messageHandlerManager,
            IMessageHandlerProvider messageHandlerProvider,
            IRabbitMqMessageConsumerManager rabbitMqMessageConsumerManager,
            IOptions<EventBusRabbitMqOptions> options,
            ILogger<RabbitMqMessageSubscribe> logger)
        {
            _messageHandlerManager = messageHandlerManager;
            _messageHandlerProvider = messageHandlerProvider;
            _rabbitMqMessageConsumerManager = rabbitMqMessageConsumerManager;
            _options = options;
            _logger = logger;
            messageHandlerManager.OnEventRemoved += SubsManager_OnEventRemoved;
        }

        private void SubsManager_OnEventRemoved(object sender, Type messageType)
        {
            lock (_lock)
            {
                var exchangeName = _options.Value.ExchangeName;
                var queueName = MessageGroupAttribute.GetGroupOrDefault(messageType);
                if (!_rabbitMqMessageConsumerManager.TryGet(exchangeName, queueName, out var rabbitMqMessageConsumer))
                    return;
                var eventName = MessageNameAttribute.GetNameOrDefault(messageType);
                rabbitMqMessageConsumer.UnbindAsync(eventName);
                if (rabbitMqMessageConsumer.HasRoutingKeyBindingQueue())
                    return;
                rabbitMqMessageConsumer.Dispose();
                _rabbitMqMessageConsumerManager.TryRemove(exchangeName, queueName);
            }
        }

        protected override void Subscribe(Type messageType, Type handlerType)
        {
            _messageHandlerManager.AddHandler(messageType, handlerType);
            TryCreateMessageConsumer(messageType);
        }

        public override void Subscribe<T, TH>()
        {
            Subscribe(typeof(T), typeof(TH));
        }

        public override void UnSubscribe<T, TH>()
        {
            _messageHandlerManager.RemoveHandler(typeof(T), typeof(TH));
        }

        private void TryCreateMessageConsumer(Type eventType)
        {
            var exchangeName = _options.Value.ExchangeName;
            var queueName = MessageGroupAttribute.GetGroupOrDefault(eventType);
            var rabbitMqMessageConsumer = _rabbitMqMessageConsumerManager.TryCreate(
                new RabbitMqExchangeDeclareConfigure(exchangeName),
               new RabbitMqQueueDeclareConfigure(queueName));
            rabbitMqMessageConsumer.OnMessageReceived(Consumer_Received);
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

            var messageType = _messageHandlerManager.MessageHandlerWrappers
                .FirstOrDefault(p => p.MessageName == eventName)
                ?.MessageType;

            if (messageType != null)
            {
                var integrationEvent = (IMessage)JsonConvert.DeserializeObject(message, messageType);

                _logger.LogTrace("Enable diagnostic listeners before consume,name is {name}", DiagnosticListenerConstants.BeforeConsume);
                EventBusDiagnosticListener.TracingConsumeBefore(integrationEvent);

                var messageHandlers = _messageHandlerProvider.GetHandlers(messageType);
                foreach (var messageHandler in messageHandlers)
                {
                    var concreteType = typeof(IMessageHandler<>).MakeGenericType(messageType);
                    var method = concreteType.GetMethod("HandAsync");
                    if (method == null) continue;
                    try
                    {
                        await (Task)method.Invoke(messageHandler, new object[] { integrationEvent });
                    }
                    catch (Exception e)
                    {
                        var handlerType = messageHandler.GetType();
                        _logger.LogError("Message processing failure,message type is {messageType},handler type is {handlerType},error message is {errorMessage}",
                            messageType, handlerType, e.Message);

                        _logger.LogTrace("Enable diagnostic listeners incorrect consume,name is {name}", DiagnosticListenerConstants.ErrorConsume);
                        EventBusDiagnosticListener.TracingConsumeError(integrationEvent, handlerType, e.Message);
                    }
                }

                _logger.LogTrace("Enable diagnostic listeners after consume,name is {name}", DiagnosticListenerConstants.AfterConsume);
                EventBusDiagnosticListener.TracingConsumeAfter(integrationEvent);
            }
            else
            {
                _logger.LogWarning("No subscription for RabbitMQ event: {eventName}", eventName);

                _logger.LogTrace("Not subscribed to enable diagnostic listener,name is {name}", DiagnosticListenerConstants.NotSubscribed);
                EventBusDiagnosticListener.TracingNotSubscribed(message);
            }
        }
    }
}
