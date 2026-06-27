using Core.EventBus.Messaging;
using Core.EventBus.Diagnostics;
using Core.RabbitMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.EventBus.Integration;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>RabbitMQ 集成事件订阅器:声明 exchange / queue / binding,并把 broker 推来的消息转给 <see cref="IMessageHandlerInvoker"/> 调用 handler。</summary>
    /// <remarks>
    /// 流程:Subscribe 注册 handler 并准备 consumer → broker 推消息 → <see cref="Consumer_Received"/> → <see cref="ProcessEvent"/> 按 routingKey 查 wrapper、反序列化、逐个 invoke。
    /// 幂等与事务一致由注入的 <see cref="IMessageHandlerInvoker"/> 决定(启用 <c>AddEfCoreEventBusStorage</c> 后自带 UoW + inbox)。
    /// ack/nack 由底层 Core.RabbitMQ 按 <see cref="EventBusRabbitMqOptions.FailureBehavior"/> 决策,handler 异常会上抛而非静默吞。
    /// </remarks>
    public class RabbitMqMessageSubscriber : MessageSubscriberBase, IIntegrationSubscriber
    {
        private readonly IIntegrationMessageHandlerManager _messageHandlerManager;
        private readonly IRabbitMqMessageConsumerManager _rabbitMqMessageConsumerManager;
        private readonly IMessageHandlerInvoker _invoker;
        private readonly IOptions<EventBusRabbitMqOptions> _options;
        private readonly ILogger<RabbitMqMessageSubscriber> _logger;
        private readonly object _lock = new();

        public RabbitMqMessageSubscriber(
            IIntegrationMessageHandlerManager messageHandlerManager,
            IRabbitMqMessageConsumerManager rabbitMqMessageConsumerManager,
            IMessageHandlerInvoker invoker,
            IOptions<EventBusRabbitMqOptions> options,
            ILogger<RabbitMqMessageSubscriber> logger)
        {
            _messageHandlerManager = messageHandlerManager;
            _rabbitMqMessageConsumerManager = rabbitMqMessageConsumerManager;
            _invoker = invoker;
            _options = options;
            _logger = logger;
            messageHandlerManager.OnEventRemoved += SubsManager_OnEventRemoved;
        }

        /// <summary>某 message type 已无 handler 订阅时,解绑对应 routing key;queue 已无任何 binding 时连 consumer 一并释放。</summary>
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

        /// <summary>按 (messageType, handlerType) 注册订阅;幂等。</summary>
        protected override async Task SubscribeAsync(Type messageType, Type handlerType, CancellationToken cancellationToken)
        {
            _messageHandlerManager.AddHandler(messageType, handlerType);
            await TryCreateMessageConsumerAsync(messageType, cancellationToken).ConfigureAwait(false);
        }

        public override Task SubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(typeof(T), typeof(TH), cancellationToken);
        }

        public override Task UnSubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
        {
            _messageHandlerManager.RemoveHandler(typeof(T), typeof(TH));
            return Task.CompletedTask;
        }

        /// <summary>为 messageType 准备消费基础设施:声明 exchange/queue、bind routing key、挂 <see cref="Consumer_Received"/> 回调。</summary>
        private async Task TryCreateMessageConsumerAsync(Type messageType, CancellationToken cancellationToken)
        {
            WarnIfFailureBehaviorWithoutDeadLetter();

            var exchangeName = _options.Value.ExchangeName;
            var queueName = MessageGroupAttribute.GetGroupOrDefault(messageType);
            var queueDeclare = new RabbitMqQueueDeclareConfigure(queueName);

            // 配置了 DLX 时写入 queue arguments;同名 queue 属性不一致会被 broker 拒绝(预期行为,让用户感知 schema 变化)
            if (!string.IsNullOrEmpty(_options.Value.DeadLetterExchange))
            {
                queueDeclare.Arguments["x-dead-letter-exchange"] = _options.Value.DeadLetterExchange;
                if (!string.IsNullOrEmpty(_options.Value.DeadLetterRoutingKey))
                {
                    queueDeclare.Arguments["x-dead-letter-routing-key"] = _options.Value.DeadLetterRoutingKey;
                }
            }

            var rabbitMqMessageConsumer = _rabbitMqMessageConsumerManager.TryCreate(
                new RabbitMqExchangeDeclareConfigure(exchangeName),
                queueDeclare);

            var eventName = MessageNameAttribute.GetNameOrDefault(messageType);
            await rabbitMqMessageConsumer.BindAsync(eventName).ConfigureAwait(false);
            rabbitMqMessageConsumer.OnMessageReceived(Consumer_Received);
        }

        /// <summary>启动期一次性告警:失败策略会丢消息(NackNoRequeue / RequeueOnce 的二次失败)但未配 DLX,丢失消息无法回溯。</summary>
        private int _failureBehaviorWarned;
        private void WarnIfFailureBehaviorWithoutDeadLetter()
        {
            if (Interlocked.Exchange(ref _failureBehaviorWarned, 1) != 0) return;

            var fb = _options.Value.FailureBehavior;
            var dropOnFailure = fb == RabbitMqFailureBehavior.NackNoRequeue || fb == RabbitMqFailureBehavior.RequeueOnce;
            if (dropOnFailure && string.IsNullOrEmpty(_options.Value.DeadLetterExchange))
            {
                _logger.LogWarning(
                    "FailureBehavior={Behavior} 在非重投路径会丢消息,但未配置 EventBus:RabbitMq:DeadLetterExchange," +
                    "二次失败/不重投的消息将被直接丢弃。建议配置 DLX 或改用 AlwaysAck。",
                    fb);
            }
        }

        /// <summary>消息抵达入口。</summary>
        /// <remarks>payload 含 "throw-fake-exception" 时强制抛异常,用于演示异常路径(重投/inbox 去重)。</remarks>
        private async Task Consumer_Received(IModel model, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            // 异常上抛:由底层 Consumer_Received 按 FailureBehavior 决定 ack/nack(重投或转 DLX),
            // 配合 inbox 去重达成最终一致
            if (message.ToLowerInvariant().Contains("throw-fake-exception"))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }
            await ProcessEvent(eventName, message);
        }

        /// <summary>反序列化 payload,查找所有订阅该 routing key 的 handler 依次调 invoker。</summary>
        /// <remarks>直接遍历 wrapper 而非 <c>IMessageHandlerProvider.GetHandlers</c>:后者会预先 resolve handler 实例,而 invoker 需在自己的新 scope 里 resolve 以保证 DbContext/UoW 生命周期。</remarks>
        private async Task ProcessEvent(string eventName, string message)
        {
            _logger.LogTrace("Processing RabbitMQ event: {eventName}", eventName);

            var wrappers = _messageHandlerManager.MessageHandlerWrappers
                .Where(p => p.MessageName == eventName)
                .OrderByDescending(p => p.HandlerPriority)
                .ToList();

            var messageType = wrappers.FirstOrDefault()?.MessageType;

            if (messageType != null)
            {
                var integrationEvent = (IMessage)JsonConvert.DeserializeObject(message, messageType);

                _logger.LogTrace("Enable diagnostic listeners before consume,name is {name}", DiagnosticListenerConstants.BeforeConsume);
                EventBusDiagnosticListener.TracingConsumeBefore(integrationEvent);

                List<Exception> handlerErrors = null;
                foreach (var wrapper in wrappers)
                {
                    try
                    {
                        // 传 HandlerType 而非已 resolve 的实例,让 invoker 在自己的 DI scope 内 resolve
                        await _invoker.InvokeAsync(messageType, wrapper.HandlerType, integrationEvent);
                    }
                    catch (Exception e)
                    {
                        // 当轮不阻断其他 handler,循环结束聚合上抛,
                        // 让底层按 FailureBehavior 决定 ack/nack 而非静默 ack 丢消息
                        _logger.LogError(e,
                            "Message processing failure: messageType={MessageType} handlerType={HandlerType}",
                            messageType, wrapper.HandlerType);

                        _logger.LogTrace("Enable diagnostic listeners incorrect consume,name is {name}", DiagnosticListenerConstants.ErrorConsume);
                        EventBusDiagnosticListener.TracingConsumeError(integrationEvent, wrapper.HandlerType, e.Message);

                        (handlerErrors ??= new List<Exception>()).Add(e);
                    }
                }

                _logger.LogTrace("Enable diagnostic listeners after consume,name is {name}", DiagnosticListenerConstants.AfterConsume);
                EventBusDiagnosticListener.TracingConsumeAfter(integrationEvent);

                if (handlerErrors != null && handlerErrors.Count > 0)
                {
                    throw new AggregateException(
                        $"{handlerErrors.Count} handler(s) failed for routingKey={eventName}",
                        handlerErrors);
                }
            }
            else
            {
                // 无订阅 = 配置遗漏或部署版本错位,建议接入告警
                _logger.LogWarning("No subscription for RabbitMQ event: {eventName}", eventName);

                _logger.LogTrace("Not subscribed to enable diagnostic listener,name is {name}", DiagnosticListenerConstants.NotSubscribed);
                EventBusDiagnosticListener.TracingNotSubscribed(message);
            }
        }
    }
}
