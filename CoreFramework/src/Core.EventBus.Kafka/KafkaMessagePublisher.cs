using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Core.EventBus.Diagnostics;
using Core.EventBus.Integration;
using Core.EventBus.Outbox;
using Core.Json.Newtonsoft;
using Core.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.EventBus.Kafka
{
    /// <summary>
    /// Kafka 集成事件 publisher，同时实现：
    /// <list type="bullet">
    ///   <item><description><see cref="IIntegrationPublisher"/>：业务侧直接调用入口（继承自 <see cref="IntegrationMessagePublisherBase"/>，
    ///   PublishAsync 会先判断是否有 outbox 上下文）</description></item>
    ///   <item><description><see cref="IOutboxRawSender"/>：outbox dispatcher 的"绕过 outbox 直发"入口</description></item>
    /// </list>
    /// 两个入口最终都走 <see cref="PublishToBroker"/>，仅 payload 来源不同。
    /// </summary>
    /// <remarks>
    /// <para><b>容错策略</b></para>
    /// <para>
    /// Kafka producer 自带重试（<c>MessageSendMaxRetries</c>）配合 <c>EnableIdempotence=true</c>
    /// 已经覆盖绝大多数瞬时网络问题；这里不再额外加 Polly 包一层。
    /// 持久错误（broker 长时间不可达、auth 错）让异常冒到上游：
    /// <list type="bullet">
    ///   <item><description>业务直发：异常冒到业务层。</description></item>
    ///   <item><description>outbox dispatcher：异常被捕获 → MarkFailed 退避循环。</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class KafkaMessagePublisher : IntegrationMessagePublisherBase, IIntegrationPublisher, IOutboxRawSender
    {
        private readonly IKafkaPersistentProducer _producer;
        private readonly IOptions<EventBusKafkaOptions> _options;
        private readonly ILogger<KafkaMessagePublisher> _logger;

        public KafkaMessagePublisher(
            IServiceScopeFactory scopeFactory,
            IKafkaPersistentProducer producer,
            IOptions<EventBusKafkaOptions> options,
            ILogger<KafkaMessagePublisher> logger)
            : base(scopeFactory)
        {
            _producer = producer;
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IntegrationMessagePublisherBase.PublishAsync{T}"/> 判定为"直发"时调用本方法。
        /// payload 来源是强类型消息对象，需要在这里序列化为 JSON。
        /// </summary>
        public override async Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            var topic = ResolveTopic(MessageNameAttribute.GetNameOrDefault(message.GetType()));
            var data = Encoding.UTF8.GetBytes(message.ToJson());
            EventBusDiagnosticListener.TracingPublishBefore(message);
            try
            {
                await PublishToBroker(message.Id, topic, data, cancellationToken).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                EventBusDiagnosticListener.TracingPublishError(message, ex.Message);
                throw;
            }
            EventBusDiagnosticListener.TracingPublishAfter(message);
        }

        /// <summary>
        /// outbox dispatcher 的直发入口。payload 已经是 outbox 表里持久化的 JSON，
        /// 不再二次序列化也不再依赖 CLR 类型 —— 这样即使生产端版本和消费端版本短暂不一致，
        /// payload 仍然能完整流到 broker。
        /// </summary>
        public async Task SendRawAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
        {
            var topic = ResolveTopic(ResolveMessageName(message));
            var data = Encoding.UTF8.GetBytes(message.MessageData);
            await PublishToBroker(message.Id, topic, data, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 与 RabbitMQ 实现的 ResolveRoutingKey 对偶：尝试用 outbox 表里的 AssemblyName+MessageName 重建 CLR 类型，
        /// 拿到上面 <c>[MessageName]</c> 特性的对外名；重建失败时回退到 outbox 存储的原值。
        /// </summary>
        private string ResolveMessageName(MessageEnvelope message)
        {
            try
            {
                var asm = System.Reflection.Assembly.Load(message.AssemblyName);
                var type = asm.GetType(message.MessageName);
                if (type != null)
                {
                    return MessageNameAttribute.GetNameOrDefault(type);
                }
            }
            catch
            {
                // 程序集找不到 / 类型已删除：按存储的原名走，消费端按 topic 名自行决定如何处理
            }
            return message.MessageName;
        }

        private string ResolveTopic(string messageName)
        {
            var prefix = _options.Value.TopicPrefix;
            return string.IsNullOrEmpty(prefix) ? messageName : prefix + messageName;
        }

        /// <summary>
        /// 真正向 Kafka broker 投递消息。两个公共入口最终都汇合到此处。
        /// </summary>
        private async Task PublishToBroker(Guid messageId, string topic, byte[] payload, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Kafka publish messageId={MessageId} topic={Topic}", messageId, topic);

            var kafkaMessage = new Message<string, byte[]>
            {
                // 用 message Id 作为 partition key，保证同一 Id 的消息（极少见的场景如重投）落在同一 partition 上有序；
                // 业务侧无 Id 排序要求时，partition 仍按 hash 均匀分布
                Key = messageId.ToString(),
                Value = payload,
                Headers = new Headers
                {
                    { "messageId", Encoding.UTF8.GetBytes(messageId.ToString()) },
                },
            };

            var report = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken).ConfigureAwait(false);

            // ProduceAsync 已经等到 ack；只有非 NoError 才会抛 ProduceException，这里 defensively 检查一次
            if (report.Status == PersistenceStatus.NotPersisted)
            {
                throw new InvalidOperationException(
                    $"Kafka message not persisted: messageId={messageId} topic={topic}");
            }
        }
    }
}
