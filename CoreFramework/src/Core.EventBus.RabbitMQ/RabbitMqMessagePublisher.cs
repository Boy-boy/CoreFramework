using Core.EventBus.Diagnostics;
using Core.EventBus.Integration;
using Core.EventBus.Outbox;
using Core.Json.Newtonsoft;
using Core.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>RabbitMQ 集成事件 publisher;同时实现 <see cref="IIntegrationPublisher"/>(业务直发)与 <see cref="IOutboxRawSender"/>(outbox dispatcher 直发),两个入口都汇合到 <see cref="PublishToBroker"/>。</summary>
    /// <remarks>
    /// 容错:Polly 对 <see cref="BrokerUnreachableException"/> / <see cref="SocketException"/> 做 3 次线性退避(1s/2s/3s),持续故障冒给调用方(业务层或 dispatcher 的 MarkFailed 退避循环)。
    /// 持久化:<c>DeliveryMode=2</c> + exchange durable 抗 broker 重启,<c>mandatory=true</c> 让无 routing 立即 return 而非静默丢弃。
    /// channel 池化:Singleton publisher 共享 <see cref="RabbitMqPublishChannelPool"/>,channel 首次创建时一次性完成 ExchangeDeclare + ConfirmSelect + BasicReturn 挂载,后续复用走纯 BasicPublish + WaitForConfirms。
    /// </remarks>
    public class RabbitMqMessagePublisher : IntegrationMessagePublisherBase, IIntegrationPublisher, IOutboxRawSender
    {
        /// <summary>Polly 重试次数,线性退避 1s/2s/3s 共约 6s,超时抛给上游。</summary>
        private readonly int _retryCount = 3;
        private readonly IRabbitMqPublishChannelPool _channelPool;
        private readonly IOptions<EventBusRabbitMqOptions> _options;
        private readonly ILogger<RabbitMqMessagePublisher> _logger;

        public RabbitMqMessagePublisher(
            IServiceProvider serviceProvider,
            IRabbitMqPublishChannelPool channelPool,
            IOptions<EventBusRabbitMqOptions> options,
            ILogger<RabbitMqMessagePublisher> logger)
        : base(serviceProvider)
        {
            _channelPool = channelPool;
            _options = options;
            _logger = logger;
        }

        /// <summary>直发路径入口:把强类型消息序列化为 JSON 后投递。</summary>
        public override async Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            var data = message.ToJson();
            var routingKey = MessageNameAttribute.GetNameOrDefault(message.GetType());
            EventBusDiagnosticListener.TracingPublishBefore(message);
            try
            {
                await PublishToBrokerAsync(message.Id, routingKey, data, cancellationToken).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                EventBusDiagnosticListener.TracingPublishError(message, ex.Message);
                throw;
            }
            EventBusDiagnosticListener.TracingPublishAfter(message);
        }

        /// <summary>outbox dispatcher 直发入口;payload 即 outbox 表里持久化的 JSON,不再二次序列化。broker header MessageId 用业务 <see cref="MessageEnvelope.MessageId"/>,与直发路径语义一致。</summary>
        /// <remarks>diagnostic 追踪与直发路径对称:listener 拿到的 MessageType 为 null(此时类型仅以 envelope.MessageName 字符串存在)。</remarks>
        public async Task SendRawAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
        {
            var routingKey = ResolveRoutingKey(message);
            EventBusDiagnosticListener.TracingPublishBefore(null);
            try
            {
                await PublishToBrokerAsync(message.MessageId, routingKey, message.MessageData, cancellationToken).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                EventBusDiagnosticListener.TracingPublishError(null, ex.Message);
                throw;
            }
            EventBusDiagnosticListener.TracingPublishAfter(null);
        }

        // outbox dispatcher 热路径上 ResolveRoutingKey 每条消息都跑;缓存按 (assembly, type) 维度,
        // 命中后省掉 Assembly.Load + GetType + Attributes 反射开销
        private static readonly ConcurrentDictionary<(string Asm, string Type), string> RoutingKeyCache = new();

        /// <summary>解析 routing key:重建 CLR 类型后取 <c>[MessageName]</c> 值,失败则回退到 envelope 里存的 CLR 全名。</summary>
        /// <remarks>
        /// 必须重建类型,否则带 <c>[MessageName("order.created")]</c> 的事件会用 CLR 全名作 routing key 而非业务名。
        /// 仅捕获程序集/类型重建相关的具体异常,其他异常透传便于排查。
        /// </remarks>
        private string ResolveRoutingKey(MessageEnvelope message)
        {
            var key = (message.AssemblyName ?? string.Empty, message.MessageName ?? string.Empty);
            return RoutingKeyCache.GetOrAdd(key, static k =>
            {
                try
                {
                    var asm = Assembly.Load(k.Item1);
                    var type = asm.GetType(k.Item2);
                    if (type != null) return MessageNameAttribute.GetNameOrDefault(type);
                }
                catch (FileNotFoundException) { /* 程序集已删除 */ }
                catch (FileLoadException) { /* 程序集加载失败 */ }
                catch (BadImageFormatException) { /* 程序集格式损坏 */ }
                catch (TypeLoadException) { /* 类型已删除/改名 */ }
                return k.Item2;
            });
        }

        /// <summary>真正向 broker 投递;两个公共入口都汇合到此,保证参数一致。</summary>
        /// <remarks>
        /// 单条流程:租 channel → BasicPublish(mandatory + DeliveryMode=2) → WaitForConfirms → 归还。
        /// ExchangeDeclare / ConfirmSelect / BasicReturn 挂载在 channel 首次创建时一次性完成。
        /// <para>Polly 切 async 后退避间隔走 <see cref="Task.Delay(TimeSpan, CancellationToken)"/>,
        /// 不再阻塞 IO 线程;BasicPublish / WaitForConfirmsOrThrow 底层仍是同步 API,但单条耗时短,可接受。</para>
        /// </remarks>
        private async Task PublishToBrokerAsync(Guid messageId, string routingKey, string payload, CancellationToken cancellationToken)
        {
            _logger.LogTrace("RabbitMQ publish messageId={MessageId} routingKey={RoutingKey}", messageId, routingKey);

            // 仅对连接级瞬时错误重试;业务错误(如 routing 失败)不重试,避免放大故障
            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(_retryCount,
                    retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                    (ex, time) =>
                    {
                        _logger.LogWarning(ex,
                            "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})",
                            messageId, $"{time.TotalSeconds:n1}", ex.Message);
                    });

            var body = Encoding.UTF8.GetBytes(payload).AsMemory();
            var exchangeName = _options.Value.ExchangeName;

            // 把 cancellationToken 传给 Polly,retry 之间检查取消,避免进程关闭时空等
            await policy.ExecuteAsync(ct =>
            {
                ct.ThrowIfCancellationRequested();

                using var rental = _channelPool.Acquire(ct);
                var channel = rental.Channel;

                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;                       // persistent:消息落 broker 磁盘
                properties.MessageId = messageId.ToString();       // 业务消息 Id,即消费端 inbox 去重键
                channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    mandatory: true,                               // routing 不到 queue 立刻 return 而非丢弃
                    basicProperties: properties,
                    body: body);

                // 等 broker 明确回执;失败(退回/nack/超时)统一抛 RabbitMqPublishFailedException,
                // 让 outbox dispatcher 据此 MarkFailed 而非误判已成功
                rental.WaitForConfirmsOrThrow(TimeSpan.FromSeconds(5));
                return Task.CompletedTask;
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
