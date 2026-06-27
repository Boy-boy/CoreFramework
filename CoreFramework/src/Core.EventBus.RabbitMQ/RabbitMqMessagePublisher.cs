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
using System.Net.Sockets;
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
            IServiceScopeFactory scopeFactory,
            IRabbitMqPublishChannelPool channelPool,
            IOptions<EventBusRabbitMqOptions> options,
            ILogger<RabbitMqMessagePublisher> logger)
        : base(scopeFactory)
        {
            _channelPool = channelPool;
            _options = options;
            _logger = logger;
        }

        /// <summary>直发路径入口:把强类型消息序列化为 JSON 后投递。</summary>
        public override Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            var data = message.ToJson();
            var routingKey = MessageNameAttribute.GetNameOrDefault(message.GetType());
            EventBusDiagnosticListener.TracingPublishBefore(message);
            try
            {
                PublishToBroker(message.Id, routingKey, data, cancellationToken);
            }
            catch (System.Exception ex)
            {
                EventBusDiagnosticListener.TracingPublishError(message, ex.Message);
                throw;
            }
            EventBusDiagnosticListener.TracingPublishAfter(message);
            return Task.CompletedTask;
        }

        /// <summary>outbox dispatcher 直发入口:payload 已是表里持久化的 JSON,不二次序列化、不依赖 CLR 类型,生产/消费版本不一致时仍能完整流到 broker。</summary>
        public Task SendRawAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
        {
            var routingKey = ResolveRoutingKey(message);
            PublishToBroker(message.Id, routingKey, message.MessageData, cancellationToken);
            return Task.CompletedTask;
        }

        /// <summary>解析 routing key:重建 CLR 类型后取 <c>[MessageName]</c> 值,失败则回退到 envelope 里存的 CLR 全名。</summary>
        /// <remarks>必须重建类型,否则带 <c>[MessageName("order.created")]</c> 的事件会用 CLR 全名作 routing key 而非业务名。</remarks>
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
                // 程序集/类型缺失:按 envelope 原名走,由消费端按 routing key 自行决定
            }
            return message.MessageName;
        }

        /// <summary>真正向 broker 投递;两个公共入口都汇合到此,保证参数一致。</summary>
        /// <remarks>单条流程:租 channel → BasicPublish(mandatory + DeliveryMode=2) → WaitForConfirms → 归还。ExchangeDeclare / ConfirmSelect / BasicReturn 挂载在 channel 首次创建时一次性完成。</remarks>
        private void PublishToBroker(Guid messageId, string routingKey, string payload, CancellationToken cancellationToken)
        {
            _logger.LogTrace("RabbitMQ publish messageId={MessageId} routingKey={RoutingKey}", messageId, routingKey);

            // 仅对连接级瞬时错误重试;业务错误(如 routing 失败)不重试,避免放大故障
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

            // 把 cancellationToken 传给 Polly,retry 之间检查取消,避免进程关闭时空等
            policy.Execute(ct =>
            {
                ct.ThrowIfCancellationRequested();

                using var rental = _channelPool.Acquire(ct);
                var channel = rental.Channel;

                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;                       // persistent:消息落 broker 磁盘
                properties.MessageId = messageId.ToString();       // 与 outbox/inbox Id 对应,便于追踪与 BasicReturn 日志关联
                channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    mandatory: true,                               // routing 不到 queue 立刻 return 而非丢弃
                    basicProperties: properties,
                    body: body);

                // 等 broker 明确回执;失败(退回/nack/超时)统一抛 RabbitMqPublishFailedException,
                // 让 outbox dispatcher 据此 MarkFailed 而非误判已成功
                rental.WaitForConfirmsOrThrow(TimeSpan.FromSeconds(5));
            }, cancellationToken);
        }
    }
}
