using Core.EventBus.Messaging;
using Core.EventBus.Diagnostics;
using Core.RabbitMQ;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.EventBus.Integration;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>RabbitMQ 集成事件订阅器:声明 exchange / queue / binding,并把 broker 推来的消息转给 <see cref="IMessageHandlerInvoker"/> 调用 handler。</summary>
    /// <remarks>
    /// 流程:Subscribe 注册 handler 并准备 consumer → broker 推消息 → <see cref="Consumer_Received"/> → <see cref="ProcessEvent"/> 按 routingKey 查 wrapper、反序列化、逐个 invoke。
    /// 幂等与事务一致由注入的 <see cref="IMessageHandlerInvoker"/> 决定(启用 <c>AddEfCoreEventBusStorage</c> 后自带 UoW + inbox)。
    /// ack/nack 由底层 Core.RabbitMQ 按 <c>EventBusRabbitMqOptions.Broker.FailureBehavior</c> 决策,handler 异常会上抛而非静默吞。
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
        /// <remarks>
        /// <see cref="IMessageHandlerManager.OnEventRemoved"/> 是 .NET 事件,签名必须同步;
        /// <c>UnbindAsync</c> 实现实际是同步完成后返回 <c>Task.CompletedTask</c>,<c>GetResult()</c> 不会阻塞 IO 线程,
        /// 但显式 await 同步获取结果可让 channel 创建 / QueueUnbind 抛出的异常正常上抛,而不是被 Task 句柄吞掉。
        /// </remarks>
        private void SubsManager_OnEventRemoved(object sender, Type messageType)
        {
            lock (_lock)
            {
                var exchangeName = _options.Value.ExchangeName;
                var queueName = MessageGroupAttribute.GetGroupOrDefault(messageType);
                if (!_rabbitMqMessageConsumerManager.TryGet(exchangeName, queueName, out var rabbitMqMessageConsumer))
                    return;
                var eventName = MessageNameAttribute.GetNameOrDefault(messageType);
                rabbitMqMessageConsumer.UnbindAsync(eventName).GetAwaiter().GetResult();
                if (rabbitMqMessageConsumer.HasAnyRoutingKey())
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
            var broker = _options.Value.Broker;
            if (broker != null && !string.IsNullOrEmpty(broker.DeadLetterExchange))
            {
                queueDeclare.Arguments["x-dead-letter-exchange"] = broker.DeadLetterExchange;
                if (!string.IsNullOrEmpty(broker.DeadLetterRoutingKey))
                {
                    queueDeclare.Arguments["x-dead-letter-routing-key"] = broker.DeadLetterRoutingKey;
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

            var broker = _options.Value.Broker;
            if (broker == null) return;
            var fb = broker.FailureBehavior;
            var dropOnFailure = fb == RabbitMqFailureBehavior.NackNoRequeue || fb == RabbitMqFailureBehavior.RequeueOnce;
            if (dropOnFailure && string.IsNullOrEmpty(broker.DeadLetterExchange))
            {
                _logger.LogWarning(
                    "FailureBehavior={Behavior} 在非重投路径会丢消息,但未配置 EventBus:RabbitMq:Broker:DeadLetterExchange," +
                    "二次失败/不重投的消息将被直接丢弃。建议配置 DLX 或改用 AlwaysAck。",
                    fb);
            }
        }

        /// <summary>消息抵达入口;handler 异常上抛由底层 Consumer 按 <c>EventBusRabbitMqOptions.Broker.FailureBehavior</c> 决定 ack/nack。</summary>
        /// <remarks>反序列化失败 / Id 校验失败 路径按 <see cref="EventBusRabbitMqOptions.PoisonMessageBehavior"/> 决策:SkipAndAck 直接 return(底层 Consumer 视作 processed → ACK);ThrowAndLetBrokerHandle 抛 <see cref="System.IO.InvalidDataException"/> 让 Consumer 走 nack/DLX。</remarks>
        private async Task Consumer_Received(IModel model, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            // publisher 把业务 IMessage.Id 写入 BasicProperties.MessageId,与 payload Id 交叉校验用
            var brokerMessageId = eventArgs.BasicProperties?.MessageId;
            await ProcessEvent(eventName, message, brokerMessageId);
        }

        /// <summary>反序列化 payload,查找所有订阅该 routing key 的 handler 依次调 invoker。</summary>
        /// <remarks>直接遍历 wrapper 而非 <c>IMessageHandlerProvider.GetHandlers</c>:后者会预先 resolve handler 实例,而 invoker 需在自己的新 scope 里 resolve 以保证 DbContext/UoW 生命周期。</remarks>
        private async Task ProcessEvent(string eventName, string message, string brokerMessageId)
        {
            _logger.LogTrace("Processing RabbitMQ event: {eventName}", eventName);

            var wrappers = _messageHandlerManager.MessageHandlerWrappers
                .Where(p => p.MessageName == eventName)
                .OrderByDescending(p => p.HandlerPriority)
                .ToList();

            var messageType = wrappers.FirstOrDefault()?.MessageType;

            if (messageType != null)
            {
                IMessage integrationEvent;
                try
                {
                    integrationEvent = (IMessage)JsonSerializer.Deserialize(message, messageType);
                }
                catch (JsonException ex)
                {
                    // payload 损坏/schema 不兼容:同一 payload 重投永远同 JsonException。
                    // 由 PoisonMessageBehavior 决定:SkipAndAck → log + return(底层 ACK 推进);
                    //                          ThrowAndLetBrokerHandle → log + 抛 InvalidDataException
                    //                          让底层按 FailureBehavior(nack/requeue/DLX)处置。
                    _logger.LogError(ex,
                        "RabbitMQ payload 反序列化失败,routingKey={RoutingKey} messageType={MessageType} payload={Payload} behavior={Behavior}",
                        eventName, messageType, TrimForLog(message), _options.Value.PoisonMessageBehavior);
                    EventBusDiagnosticListener.TracingConsumeError(null, null, ex.Message);
                    HandlePoisonMessage(ex, "deserialization failed");
                    return;
                }

                // 反序列化产物身份校验:null / 缺 Id 走 PoisonMessageBehavior 同一路径
                if (!ValidateIdentity(integrationEvent, brokerMessageId, eventName, messageType, message))
                {
                    return;
                }

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

                if (handlerErrors != null && handlerErrors.Count > 0)
                {
                    // 不发 AfterConsume:监控混淆 ErrorConsume + AfterConsume 会误判为"消费成功"
                    throw new AggregateException(
                        $"{handlerErrors.Count} handler(s) failed for routingKey={eventName}",
                        handlerErrors);
                }

                _logger.LogTrace("Enable diagnostic listeners after consume,name is {name}", DiagnosticListenerConstants.AfterConsume);
                EventBusDiagnosticListener.TracingConsumeAfter(integrationEvent);
            }
            else
            {
                // 无订阅 = 配置遗漏或部署版本错位,建议接入告警
                _logger.LogWarning("No subscription for RabbitMQ event: {eventName}", eventName);

                _logger.LogTrace("Not subscribed to enable diagnostic listener,name is {name}", DiagnosticListenerConstants.NotSubscribed);
                EventBusDiagnosticListener.TracingNotSubscribed(message);
            }
        }

        /// <summary>截断超长 payload,防反序列化失败日志被异常大消息撑爆。</summary>
        private static string TrimForLog(string payload)
        {
            const int MaxLogPayload = 512;
            if (string.IsNullOrEmpty(payload) || payload.Length <= MaxLogPayload) return payload;
            return payload.Substring(0, MaxLogPayload) + "...(truncated)";
        }

        /// <summary>按 <see cref="EventBusRabbitMqOptions.PoisonMessageBehavior"/> 处置毒消息:抛/吞。</summary>
        /// <remarks>抛出时统一包装为 <see cref="InvalidDataException"/>,底层 Consumer 按 FailureBehavior nack/requeue/DLX。</remarks>
        private void HandlePoisonMessage(Exception cause, string reason)
        {
            if (_options.Value.PoisonMessageBehavior == PoisonMessageBehavior.ThrowAndLetBrokerHandle)
            {
                throw new InvalidDataException(
                    $"Poison message detected ({reason}); rethrowing per PoisonMessageBehavior=ThrowAndLetBrokerHandle.",
                    cause);
            }
            // SkipAndAck: do nothing, caller returns and底层 ACK 推进 offset
        }

        /// <summary>校验并对齐消息身份:优先以 brokerMessageId 为权威 Id;否则用 raw JSON 验证 payload 显式带 Id 字段。</summary>
        /// <remarks>
        /// <para>关键风险:<see cref="Message"/> 基类构造里 <c>Id = Guid.CreateVersion7()</c>。
        /// 若外部消息 payload 没有 Id 字段,System.Text.Json 用基类构造生成的新 Guid 填充 → 看起来"非 Empty"但其实
        /// 每次反序列化都是不同值 → inbox 按 Id 去重彻底失效 → 重投会全部当新消息处理。</para>
        /// <para>处理策略(按优先级):</para>
        /// <list type="number">
        ///   <item><description>broker 端有效 Guid:权威 Id,直接覆盖 payload Id(publisher 写入,可信)。不一致 warn。</description></item>
        ///   <item><description>broker 端缺失/非 Guid:检查 raw JSON 是否显式含 Id 字段。无 → reject(同 poison)。</description></item>
        ///   <item><description>有显式 Id 但反序列化后是 Empty:reject。</description></item>
        /// </list>
        /// </remarks>
        private bool ValidateIdentity(IMessage integrationEvent, string brokerMessageId, string eventName, Type messageType, string rawPayload)
        {
            if (integrationEvent == null)
            {
                _logger.LogError(
                    "RabbitMQ payload 反序列化为 null,routingKey={RoutingKey} messageType={MessageType} payload={Payload} behavior={Behavior}",
                    eventName, messageType, TrimForLog(rawPayload), _options.Value.PoisonMessageBehavior);
                EventBusDiagnosticListener.TracingConsumeError(null, null, "deserialized payload is null");
                HandlePoisonMessage(null, "deserialized payload is null");
                return false;
            }

            // 路径 1:broker 端写了合法 Guid → 权威,覆盖 payload Id
            if (!string.IsNullOrEmpty(brokerMessageId) && Guid.TryParse(brokerMessageId, out var brokerId))
            {
                if (brokerId == Guid.Empty)
                {
                    _logger.LogError(
                        "RabbitMQ brokerMessageId 为 Guid.Empty,routingKey={RoutingKey} messageType={MessageType} behavior={Behavior}",
                        eventName, messageType, _options.Value.PoisonMessageBehavior);
                    EventBusDiagnosticListener.TracingConsumeError(integrationEvent, null, "brokerMessageId is Guid.Empty");
                    HandlePoisonMessage(null, "brokerMessageId is Guid.Empty");
                    return false;
                }
                if (integrationEvent.Id != brokerId)
                {
                    // 可能是外部消息没带 Id(基类构造生成了新 Guid),也可能是 publisher 改实现 / 中间件改写
                    _logger.LogWarning(
                        "RabbitMQ payload Id={PayloadId} 与 brokerMessageId={BrokerMessageId} 不一致,以 brokerMessageId 为准 routingKey={RoutingKey}",
                        integrationEvent.Id, brokerMessageId, eventName);
                    integrationEvent.Id = brokerId;
                }
                return true;
            }

            // 路径 2:broker 端缺失/非 Guid → 必须从 raw JSON 验证 payload 显式带 Id
            // 否则 Message 基类构造生成的随机 Guid 会让 inbox 去重失效
            if (!HasExplicitIdField(rawPayload))
            {
                _logger.LogError(
                    "RabbitMQ payload 没有显式 Id 字段且 brokerMessageId 不可用,inbox 去重会失效,routingKey={RoutingKey} messageType={MessageType} payload={Payload} behavior={Behavior}",
                    eventName, messageType, TrimForLog(rawPayload), _options.Value.PoisonMessageBehavior);
                EventBusDiagnosticListener.TracingConsumeError(integrationEvent, null, "payload missing explicit Id field");
                HandlePoisonMessage(null, "payload missing explicit Id field");
                return false;
            }

            if (integrationEvent.Id == Guid.Empty)
            {
                _logger.LogError(
                    "RabbitMQ payload Id 字段值为 Guid.Empty,inbox 去重会失效,routingKey={RoutingKey} messageType={MessageType} behavior={Behavior}",
                    eventName, messageType, _options.Value.PoisonMessageBehavior);
                EventBusDiagnosticListener.TracingConsumeError(integrationEvent, null, "payload Id is Guid.Empty");
                HandlePoisonMessage(null, "payload Id is Guid.Empty");
                return false;
            }

            return true;
        }

        /// <summary>检查 raw JSON 顶层是否显式包含非空 Id 字段(大小写不敏感)。</summary>
        /// <remarks>解析失败/格式异常视为"没有显式 Id";调用方据此 reject。</remarks>
        private static bool HasExplicitIdField(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return false;
            try
            {
                using var document = JsonDocument.Parse(rawJson);
                if (document.RootElement.ValueKind != JsonValueKind.Object) return false;

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (!property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;
                    if (property.Value.ValueKind == JsonValueKind.Null) return false;
                    return property.Value.ValueKind != JsonValueKind.String
                           || !string.IsNullOrWhiteSpace(property.Value.GetString());
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
