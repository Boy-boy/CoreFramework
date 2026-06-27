using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Core.EventBus.Diagnostics;
using Core.EventBus.Integration;
using Core.EventBus.Messaging;
using Core.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Core.EventBus.Kafka
{
    /// <summary>
    /// Kafka 集成事件订阅器:按 <see cref="MessageGroupAttribute"/> 拆 consumer group,
    /// 按 <see cref="MessageNameAttribute"/> 解析 topic,把 broker 推来的消息交给
    /// <see cref="IMessageHandlerInvoker"/>。
    /// </summary>
    /// <remarks>
    /// <para>幂等与事务一致性由注入的 <see cref="IMessageHandlerInvoker"/> 决定;
    /// 启用 <c>AddEfCoreEventBusStorage</c> 后自动具备 UoW + inbox 包装。</para>
    /// <para>单 handler 失败不阻断同条内其他 handler;循环结束聚合 <see cref="AggregateException"/> 上抛,
    /// PollLoop 据此跳过 commit + Seek 回失败 offset 让 broker 重投。</para>
    /// <para>Kafka 消费失败粒度是 partition 单点 offset,<b>无法选择性重投单条</b>:
    /// 同 partition 的下一条会被推迟到失败那条成功后才推进。poison message 需要上层 inbox
    /// 跟踪重试 + 业务层显式投递 dead-letter topic 兜底。</para>
    /// </remarks>
    public class KafkaMessageSubscriber : MessageSubscriberBase, IIntegrationSubscriber
    {
        private readonly IIntegrationMessageHandlerManager _messageHandlerManager;
        private readonly IKafkaMessageConsumerManager _kafkaConsumerManager;
        private readonly IMessageHandlerInvoker _invoker;
        private readonly IOptions<EventBusKafkaOptions> _options;
        private readonly ILogger<KafkaMessageSubscriber> _logger;
        private readonly object _lock = new();

        public KafkaMessageSubscriber(
            IIntegrationMessageHandlerManager messageHandlerManager,
            IKafkaMessageConsumerManager kafkaConsumerManager,
            IMessageHandlerInvoker invoker,
            IOptions<EventBusKafkaOptions> options,
            ILogger<KafkaMessageSubscriber> logger)
        {
            _messageHandlerManager = messageHandlerManager;
            _kafkaConsumerManager = kafkaConsumerManager;
            _invoker = invoker;
            _options = options;
            _logger = logger;
            messageHandlerManager.OnEventRemoved += SubsManager_OnEventRemoved;
        }

        /// <summary>
        /// 某个 message type 已无任何 handler 时,从对应 consumer group 移除 topic;
        /// group 无任何 topic 时释放整个 consumer。
        /// </summary>
        /// <remarks>
        /// <see cref="IMessageHandlerManager.OnEventRemoved"/> 是 .NET 事件必须同步签名;
        /// <c>UnsubscribeTopicAsync</c> 实现是即时返回 CompletedTask 的伪 async,<c>GetResult()</c> 不会阻塞 IO 线程。
        /// </remarks>
        private void SubsManager_OnEventRemoved(object sender, Type messageType)
        {
            lock (_lock)
            {
                var groupId = MessageGroupAttribute.GetGroupOrDefault(messageType);
                if (!_kafkaConsumerManager.TryGet(groupId, out var consumer))
                    return;
                var topic = ResolveTopic(MessageNameAttribute.GetNameOrDefault(messageType));
                consumer.UnsubscribeTopicAsync(topic).GetAwaiter().GetResult();
                if (consumer.HasAnyTopic())
                    return;
                consumer.Dispose();
                _kafkaConsumerManager.TryRemove(groupId);
            }
        }

        /// <summary>启动时按 (messageType, handlerType) 注册订阅;幂等。</summary>
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
        /// 为 message type 准备 Kafka 消费基础设施:解析 (group.id, topic),取或建 consumer
        /// (可选显式声明 topic),加入订阅集合,挂上 <see cref="Consumer_Received"/> 回调。
        /// </summary>
        private async Task TryCreateMessageConsumerAsync(Type messageType, CancellationToken cancellationToken)
        {
            var groupId = MessageGroupAttribute.GetGroupOrDefault(messageType);
            var topic = ResolveTopic(MessageNameAttribute.GetNameOrDefault(messageType));

            KafkaTopicDeclareConfigure topicDeclare = null;
            if (_options.Value.DeclareTopicsOnSubscribe)
            {
                topicDeclare = new KafkaTopicDeclareConfigure(topic,
                    _options.Value.DefaultPartitionCount,
                    _options.Value.DefaultReplicationFactor);
            }

            var consumer = _kafkaConsumerManager.TryCreate(groupId, topicDeclare);
            consumer.OnMessageReceived(Consumer_Received);
            await consumer.SubscribeTopicAsync(topic).ConfigureAwait(false);
        }

        private string ResolveTopic(string messageName)
        {
            var prefix = _options.Value.TopicPrefix;
            return string.IsNullOrEmpty(prefix) ? messageName : prefix + messageName;
        }

        /// <summary>Kafka 消息抵达入口。</summary>
        /// <remarks>
        /// "throw-fake-exception" 特判用于测试:payload 含该字符串强制抛异常,演示
        /// PollLoop 跳过 commit + Seek 回 offset → broker 重投的路径。
        /// </remarks>
        private Task Consumer_Received(IConsumer<string, byte[]> consumer, ConsumeResult<string, byte[]> result)
        {
            var topic = result.Topic;
            var payload = result.Message.Value;
            var message = Encoding.UTF8.GetString(payload);
            if (message.ToLowerInvariant().Contains("throw-fake-exception"))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }
            return ProcessEvent(topic, message);
        }

        /// <summary>反序列化 payload,查找订阅该 topic 的所有 handler,依次调用 invoker。</summary>
        /// <remarks>
        /// <b>直接遍历 wrapper 而非走 <c>IMessageHandlerProvider.GetHandlers</c></b>:
        /// provider 会在 wrapper leak 的 scope 里预先 resolve handler 实例,
        /// 而 invoker 需要在自己的新 scope 里 resolve,以保证 DbContext / UoW 生命周期正确。
        /// </remarks>
        private async Task ProcessEvent(string topic, string message)
        {
            _logger.LogTrace("Processing Kafka event: topic={Topic}", topic);

            // 把 topic 反推回 messageName(去掉前缀),用 messageName 查 wrapper
            var prefix = _options.Value.TopicPrefix;
            var messageName = !string.IsNullOrEmpty(prefix) && topic.StartsWith(prefix)
                ? topic.Substring(prefix.Length)
                : topic;

            var wrappers = _messageHandlerManager.MessageHandlerWrappers
                .Where(p => p.MessageName == messageName)
                .OrderByDescending(p => p.HandlerPriority)
                .ToList();

            var messageType = wrappers.FirstOrDefault()?.MessageType;

            if (messageType != null)
            {
                IMessage integrationEvent;
                try
                {
                    integrationEvent = (IMessage)JsonConvert.DeserializeObject(message, messageType);
                }
                catch (JsonException ex)
                {
                    // schema 不兼容/payload 损坏:Kafka 字节固定,重试永远同 JsonException。
                    // 直接 log + return → PollLoop 视作成功 commit 推进,避免无限 Seek 卡死 partition。
                    // 业务想要"可观测的丢失"只能靠 ops 监控这条 LogError 或上游接 Schema Registry。
                    _logger.LogError(ex,
                        "Kafka payload 反序列化失败,跳过该条 offset topic={Topic} messageType={MessageType} payload={Payload}",
                        topic, messageType, TrimForLog(message));
                    EventBusDiagnosticListener.TracingConsumeError(null, null, ex.Message);
                    return;
                }

                _logger.LogTrace("Enable diagnostic listeners before consume,name is {name}", DiagnosticListenerConstants.BeforeConsume);
                EventBusDiagnosticListener.TracingConsumeBefore(integrationEvent);

                List<Exception> handlerErrors = null;
                foreach (var wrapper in wrappers)
                {
                    try
                    {
                        // 传 HandlerType 而非已 resolve 的实例:invoker 在自己的 DI scope 内 resolve,保证生命周期一致
                        await _invoker.InvokeAsync(messageType, wrapper.HandlerType, integrationEvent).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        // 当轮不阻断其他 handler,循环结束聚合上抛 → PollLoop 跳过 commit + Seek 回 offset,broker 重投同条;
                        // 配合 inbox 去重让已成功的 handler 在重投时跳过,只重跑失败那个。
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
                        $"{handlerErrors.Count} handler(s) failed for topic={topic}",
                        handlerErrors);
                }
            }
            else
            {
                _logger.LogWarning("No subscription for Kafka event: topic={Topic}", topic);

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
    }
}
