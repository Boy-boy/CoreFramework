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
    /// <summary>
    /// RabbitMQ 集成事件订阅器：负责声明 exchange / queue / binding，并把 broker 推来的
    /// 消息转交给 <see cref="IMessageHandlerInvoker"/> 调用 handler。
    /// </summary>
    /// <remarks>
    /// <para><b>订阅流程</b></para>
    /// <list type="number">
    ///   <item><description><see cref="Subscribe(Type, Type)"/>：注册 handler 到 manager 并声明 RabbitMQ 消费者</description></item>
    ///   <item><description>broker 推消息 → <see cref="Consumer_Received"/> → <see cref="ProcessEvent"/></description></item>
    ///   <item><description>按 routingKey 查 wrapper → 反序列化 payload → 逐个 handler 调 <see cref="IMessageHandlerInvoker.InvokeAsync"/></description></item>
    /// </list>
    ///
    /// <para><b>幂等与事务一致性</b></para>
    /// <para>
    /// 这两点由注入的 <see cref="IMessageHandlerInvoker"/> 决定，订阅器本身不感知。
    /// 启用 <c>AddEfCoreEventBusStorage</c> 后 invoker 自动具备 UoW + inbox 包装。
    /// </para>
    ///
    /// <para><b>异常处理（当前实现）</b></para>
    /// <para>
    /// <see cref="Consumer_Received"/> 的最外层 catch 仍然<b>静默吞掉异常并记 LogWarning</b>，
    /// 这是为了保持兼容旧版行为。<b>语义警告</b>：当前未做 broker ack 控制，意味着
    /// 异常时消息可能被自动 ack 而丢失。生产环境建议结合 RabbitMQ 的 manual ack 配置
    /// （未在本类范围内）使语义更严格。
    /// </para>
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

        /// <summary>
        /// 当 manager 通知某个 message type 已没有任何 handler 订阅时，
        /// 解绑对应的 RabbitMQ routing key；若 queue 已无任何 binding，连 consumer 一并释放。
        /// </summary>
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

        /// <summary>启动时按 (messageType, handlerType) 注册订阅；幂等。</summary>
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

        /// <summary>
        /// 为一个 message type 准备好 RabbitMQ 消费基础设施：
        /// <list type="bullet">
        ///   <item><description>声明 exchange / queue（如不存在）</description></item>
        ///   <item><description>把 routing key bind 到 queue</description></item>
        ///   <item><description>挂上 <see cref="Consumer_Received"/> 作为消息回调</description></item>
        /// </list>
        /// </summary>
        private async Task TryCreateMessageConsumerAsync(Type messageType, CancellationToken cancellationToken)
        {
            WarnIfFailureBehaviorWithoutDeadLetter();

            var exchangeName = _options.Value.ExchangeName;
            var queueName = MessageGroupAttribute.GetGroupOrDefault(messageType);
            var queueDeclare = new RabbitMqQueueDeclareConfigure(queueName);

            // 若配置了 DLX,把它写入 queue 声明的 arguments;
            // 已存在的同名 queue 若属性不一致会被 broker 拒绝,这是预期行为 —— 让用户明确感知到 schema 变化
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

        /// <summary>
        /// 启动期一次性告警:若失败策略会丢消息(NackNoRequeue / RequeueOnce 的二次失败)但没配 DLX,
        /// 二次失败的消息会被 broker 直接丢弃,运维侧无法回溯。
        /// </summary>
        private int _failureBehaviorWarned;
        private void WarnIfFailureBehaviorWithoutDeadLetter()
        {
            if (Interlocked.Exchange(ref _failureBehaviorWarned, 1) != 0) return;

            var fb = _options.Value.FailureBehavior;
            var dropOnFailure = fb == RabbitMqFailureBehavior.NackNoRequeue || fb == RabbitMqFailureBehavior.RequeueOnce;
            if (dropOnFailure && string.IsNullOrEmpty(_options.Value.DeadLetterExchange))
            {
                _logger.LogWarning(
                    "FailureBehavior={Behavior} 在非重投路径会丢消息,但未配置 EventBus:RabbitMq:DeadLetterExchange。" +
                    "二次失败/不重投的消息将被 broker 直接丢弃,运维侧无法回溯。建议配置 DLX 或改用 AlwaysAck。",
                    fb);
            }
        }

        /// <summary>
        /// RabbitMQ 消息抵达入口。
        /// </summary>
        /// <remarks>
        /// "throw-fake-exception" 的特判用于测试：在 payload 里包含该字符串可强制抛异常，
        /// 用来演示异常路径（broker 重投、inbox 去重等）。
        /// </remarks>
        private async Task Consumer_Received(IModel model, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            // 异常不再吞:Core.RabbitMQ 的 Consumer_Received 会拿到这个异常并按
            // RabbitMqOptions.FailureBehavior 决定 ack / nack(重投或转 DLX),
            // 配合上层 inbox 去重达成"业务最终一致"。
            if (message.ToLowerInvariant().Contains("throw-fake-exception"))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }
            await ProcessEvent(eventName, message);
        }

        /// <summary>
        /// 反序列化 payload，查找所有订阅了该 routing key 的 handler，依次调用 invoker。
        /// </summary>
        /// <remarks>
        /// 这里<b>直接遍历 wrapper 而不是用 <c>IMessageHandlerProvider.GetHandlers</c></b>，
        /// 原因是 provider 会预先 resolve handler 实例（在 wrapper 的 leak 的 scope 里），
        /// 而 invoker 需要在自己的新 scope 里 resolve，以保证 DbContext / UoW 生命周期正确。
        /// </remarks>
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
                        // 注意：传 HandlerType 而非已 resolve 的 handler 实例。
                        // invoker 会在自己的 DI scope 内 resolve，保证生命周期一致
                        await _invoker.InvokeAsync(messageType, wrapper.HandlerType, integrationEvent);
                    }
                    catch (Exception e)
                    {
                        // 当轮不阻断其他 handler,但循环结束聚合上抛 ——
                        // 让底层 Consumer_Received 按 FailureBehavior 决定 ack/nack,
                        // 而不是像旧版那样静默 ack 导致消息丢失
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
                // 没有订阅 = 配置遗漏或部署版本错位。typically 应该告警而不只是 log warning
                _logger.LogWarning("No subscription for RabbitMQ event: {eventName}", eventName);

                _logger.LogTrace("Not subscribed to enable diagnostic listener,name is {name}", DiagnosticListenerConstants.NotSubscribed);
                EventBusDiagnosticListener.TracingNotSubscribed(message);
            }
        }
    }
}
