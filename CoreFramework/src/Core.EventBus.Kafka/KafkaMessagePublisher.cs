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
    /// Kafka 集成事件 publisher;同时实现 <see cref="IIntegrationPublisher"/>(业务直发)
    /// 和 <see cref="IOutboxRawSender"/>(outbox dispatcher 直发),最终都汇合到 <see cref="PublishToBroker"/>。
    /// </summary>
    /// <remarks>
    /// 容错依赖 Kafka producer 自带重试 + <c>EnableIdempotence=true</c>,不再额外加 Polly。
    /// 持久错误异常上抛:业务直发冒到业务层;outbox 直发由 dispatcher 捕获后 MarkFailed 退避。
    /// </remarks>
    public class KafkaMessagePublisher : IntegrationMessagePublisherBase, IIntegrationPublisher, IOutboxRawSender
    {
        private readonly IKafkaPersistentProducer _producer;
        private readonly IOptions<EventBusKafkaOptions> _options;
        private readonly ILogger<KafkaMessagePublisher> _logger;

        public KafkaMessagePublisher(
            IServiceProvider serviceProvider,
            IKafkaPersistentProducer producer,
            IOptions<EventBusKafkaOptions> options,
            ILogger<KafkaMessagePublisher> logger)
            : base(serviceProvider)
        {
            _producer = producer;
            _options = options;
            _logger = logger;
        }

        /// <summary>业务直发入口;payload 是强类型对象,这里序列化为 JSON。</summary>
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
        /// outbox dispatcher 直发入口;payload 已是 outbox 表里持久化的 JSON,
        /// 不二次序列化也不依赖 CLR 类型,保证生产/消费端版本短暂不一致时 payload 仍能流到 broker。
        /// </summary>
        public async Task SendRawAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
        {
            var topic = ResolveTopic(ResolveMessageName(message));
            var data = Encoding.UTF8.GetBytes(message.MessageData);
            await PublishToBroker(message.Id, topic, data, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 用 outbox 表里的 AssemblyName+MessageName 重建 CLR 类型,取 <c>[MessageName]</c> 对外名;
        /// 重建失败回退到 outbox 存储的原值。
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
                // 程序集找不到/类型已删除:按存储原名走,消费端按 topic 名自行处理
            }
            return message.MessageName;
        }

        private string ResolveTopic(string messageName)
        {
            var prefix = _options.Value.TopicPrefix;
            return string.IsNullOrEmpty(prefix) ? messageName : prefix + messageName;
        }

        /// <summary>真正向 Kafka broker 投递消息;两个公共入口最终汇合到此。</summary>
        private async Task PublishToBroker(Guid messageId, string topic, byte[] payload, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Kafka publish messageId={MessageId} topic={Topic}", messageId, topic);

            var kafkaMessage = new Message<string, byte[]>
            {
                // 用 message Id 作 partition key,保证同 Id 消息(如重投)落在同一 partition 上有序
                Key = messageId.ToString(),
                Value = payload,
                Headers = new Headers
                {
                    { "messageId", Encoding.UTF8.GetBytes(messageId.ToString()) },
                },
            };

            var report = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken).ConfigureAwait(false);

            // ProduceAsync 已等到 ack;只有非 NoError 才会抛 ProduceException,这里 defensively 检查一次
            if (report.Status == PersistenceStatus.NotPersisted)
            {
                throw new InvalidOperationException(
                    $"Kafka message not persisted: messageId={messageId} topic={topic}");
            }
        }
    }
}
