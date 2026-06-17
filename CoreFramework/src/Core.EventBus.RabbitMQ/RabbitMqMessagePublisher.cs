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
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>
    /// RabbitMQ 集成事件 publisher，同时实现：
    /// <list type="bullet">
    ///   <item><description><see cref="IIntegrationMessagePublisher"/>：业务侧直接调用入口（继承自 <see cref="IntegrationMessagePublisherBase"/>，
    ///   PublishAsync 会先判断是否有 outbox 上下文）</description></item>
    ///   <item><description><see cref="IOutboxRawSender"/>：outbox dispatcher 的"绕过 outbox 直发"入口</description></item>
    /// </list>
    /// 两个入口最终都走 <see cref="PublishToBroker"/>，仅 payload 来源不同。
    /// </summary>
    /// <remarks>
    /// <para><b>容错策略</b></para>
    /// <list type="bullet">
    ///   <item><description>Polly 在 <see cref="BrokerUnreachableException"/> / <see cref="SocketException"/> 时
    ///   做 3 次线性退避重试（1s/2s/3s）—— 应对短暂网络抖动</description></item>
    ///   <item><description>更大范围的故障（持续不可达、消息持续被拒）由调用方处理：
    ///     <list type="bullet">
    ///       <item><description>业务调用方（PublishAsync 直发路径）：异常冒到业务层</description></item>
    ///       <item><description>dispatcher 调用方（SendRawAsync）：异常被 dispatcher 捕获 → 进 MarkFailed 退避循环</description></item>
    ///     </list>
    ///   </description></item>
    /// </list>
    ///
    /// <para><b>持久化</b></para>
    /// <para>
    /// <c>DeliveryMode = 2</c> + <c>exchange durable: true</c> 让消息在 broker 重启后不丢；
    /// <c>mandatory: true</c> 在 routing 不到任何 queue 时立即 return 而不是静默丢弃。
    /// </para>
    /// </remarks>
    public class RabbitMqMessagePublisher : IntegrationMessagePublisherBase, IIntegrationMessagePublisher, IOutboxRawSender
    {
        /// <summary>Polly 重试次数；线性退避 1s/2s/3s 总等约 6 秒，超过则抛给上游。</summary>
        private readonly int _retryCount = 3;
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly IOptions<EventBusRabbitMqOptions> _options;
        private readonly ILogger<RabbitMqMessagePublisher> _logger;

        public RabbitMqMessagePublisher(
            IServiceScopeFactory scopeFactory,
            IRabbitMqPersistentConnection persistentConnection,
            IOptions<EventBusRabbitMqOptions> options,
            ILogger<RabbitMqMessagePublisher> logger)
        : base(scopeFactory)
        {
            _persistentConnection = persistentConnection;
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IntegrationMessagePublisherBase.PublishAsync{T}"/> 判定为"直发"时调用本方法。
        /// payload 来源是强类型消息对象，需要在这里序列化为 JSON。
        /// </summary>
        public override Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            var data = message.ToJson();
            var routingKey = MessageNameAttribute.GetNameOrDefault(message.GetType());
            PublishToBroker(message.Id, routingKey, data);
            EventBusDiagnosticListener.TracingPublishBefore(message);
            EventBusDiagnosticListener.TracingPublishAfter(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// outbox dispatcher 的直发入口。payload 已经是 outbox 表里持久化的 JSON，
        /// 不再二次序列化也不再依赖 CLR 类型 —— 这样即使生产端版本和消费端版本短暂不一致，
        /// payload 仍然能完整流到 broker。
        /// </summary>
        public Task SendRawAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
        {
            var routingKey = ResolveRoutingKey(message);
            PublishToBroker(message.Id, routingKey, message.MessageData);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 解析 routing key：尝试 <c>Assembly.Load + GetType</c> 重建 CLR 类型，
        /// 用上面 <c>[MessageName]</c> 特性返回的值；如果重建失败（类型已重命名/移除）
        /// 则回退到 outbox 表里存的 <see cref="MessageEnvelope.MessageName"/>（CLR 全名）。
        /// </summary>
        /// <remarks>
        /// 之所以要重建类型而不直接用 <see cref="MessageEnvelope.MessageName"/>：
        /// 因为业务可能在事件类上加 <c>[MessageName("order.created")]</c>，
        /// 那么 broker routing key 应该是 "order.created" 而不是 CLR 全名。
        /// </remarks>
        private string ResolveRoutingKey(MessageEnvelope message)
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
                // 程序集找不到、类型已删除等：不报错，按存储的原名走 —— 让消费端按 routing key 自行决定如何处理
            }
            return message.MessageName;
        }

        /// <summary>
        /// 真正向 broker 投递消息。两个公共入口最终都汇合到此处，保证投递参数完全一致。
        /// </summary>
        private void PublishToBroker(Guid messageId, string routingKey, string payload)
        {
            _logger.LogTrace("RabbitMQ publish messageId={MessageId} routingKey={RoutingKey}", messageId, routingKey);

            // 仅对"连接级"瞬时错误重试。业务错误（如 routing 失败）不在此重试，否则会放大故障
            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount,
                    retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                    (ex, time) =>
                    {
                        _logger.LogWarning(ex,
                            "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})",
                            messageId, $"{time.TotalSeconds:n1}", ex.Message);
                    });

            var body = Encoding.UTF8.GetBytes(payload).AsMemory();
            var exchangeName = _options.Value.ExchangeName;

            policy.Execute(() =>
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                using var channel = _persistentConnection.CreateModel();
                channel.ExchangeDeclare(
                    exchange: exchangeName,
                    type: "direct",
                    durable: true,                                 // broker 重启后 exchange 仍在
                    autoDelete: false,
                    arguments: new ConcurrentDictionary<string, object>());

                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;                       // persistent：消息落 broker 磁盘
                properties.MessageId = messageId.ToString();       // 与 outbox / inbox 的 Id 对应，便于追踪
                channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    mandatory: true,                               // routing 不到 queue 立刻 return 而非丢弃
                    basicProperties: properties,
                    body: body);
            });
        }
    }
}
