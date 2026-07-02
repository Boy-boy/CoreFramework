using Core.RabbitMQ;
using System;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>RabbitMQ broker 配置;绑定 <c>EventBus:RabbitMq</c> 节点。</summary>
    /// <remarks>
    /// 连接 / 失败策略 / DLX 一组字段嵌套在 <see cref="Broker"/> 子对象里(<see cref="RabbitMqOptions"/> 原样复用),
    /// 启动时 EventBus 层把 <see cref="Broker"/> 字段透传给 <c>IOptions&lt;RabbitMqOptions&gt;</c>;
    /// 这样底层 <see cref="RabbitMqOptions"/> 新增字段无需 EventBus 层跟改、appsettings 也无须写两遍。
    /// </remarks>
    public class EventBusRabbitMqOptions
    {
        private string _defaultExchangeName = "event_bus_default_routing";

        public EventBusRabbitMqOptions()
        {
            Broker = new RabbitMqOptions();
        }

        /// <summary>全局 direct exchange 名,按 <see cref="MessageNameAttribute"/> 作为 routing key 绑定到各 queue;不可为 null。</summary>
        public string ExchangeName
        {
            get => _defaultExchangeName;
            set => _defaultExchangeName = value ?? throw new ArgumentNullException(nameof(value), "ExchangeName 不能为 null;若需默认值请使用 EventBusRabbitMqOptions 的默认实例。");
        }

        /// <summary>publisher channel 池上限,即同时刻最多并发 publish 数;默认 8。</summary>
        /// <remarks>channel 首次创建时一次性完成 ExchangeDeclare + ConfirmSelect + BasicReturn 挂载,后续 publish 复用走纯 BasicPublish + WaitForConfirms;超出本值的并发请求由 SemaphoreSlim 排队。</remarks>
        public int ChannelPoolSize { get; set; } = 8;

        /// <summary>
        /// publisher confirms 等待 broker ack/nack/return 的超时;默认 5s。<br/>
        /// 跨云 / 跨机房链路应放宽到 10~30s;单机 broker 可缩到 2s。超时后统一抛
        /// <see cref="RabbitMqPublishUnconfirmedException"/>(TimedOut=true),outbox dispatcher 会 MarkFailed。
        /// </summary>
        public TimeSpan PublishConfirmTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>反序列化失败 / Id 校验失败的"毒消息"处置策略;默认 <see cref="PoisonMessageBehavior.SkipAndAck"/>。</summary>
        /// <remarks>需要 DLX 收集毒消息时改为 <see cref="PoisonMessageBehavior.ThrowAndLetBrokerHandle"/> 并配合 <c>Broker.FailureBehavior=NackNoRequeue</c> + <c>Broker.DeadLetterExchange</c>。</remarks>
        public PoisonMessageBehavior PoisonMessageBehavior { get; set; } = PoisonMessageBehavior.SkipAndAck;

        /// <summary>底层 RabbitMQ 模块配置(连接 / 失败策略 / DLX);绑定 <c>EventBus:RabbitMq:Broker</c> 节点。</summary>
        /// <remarks>EventBus 层不再单独声明 Connection / FailureBehavior / DLX 字段,统一以 <see cref="RabbitMqOptions"/> 原型为准 —— 底层新增字段会自动透传到 <c>IOptions&lt;RabbitMqOptions&gt;</c>。</remarks>
        public RabbitMqOptions Broker { get; set; }

        /// <summary>启动期校验,数值非法立刻抛 <see cref="InvalidOperationException"/>。</summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ExchangeName))
                throw new InvalidOperationException(
                    $"{nameof(EventBusRabbitMqOptions)}.{nameof(ExchangeName)} 不能为空。");
            if (ChannelPoolSize <= 0)
                throw new InvalidOperationException(
                    $"{nameof(EventBusRabbitMqOptions)}.{nameof(ChannelPoolSize)} 必须 > 0,当前={ChannelPoolSize}。");
            if (PublishConfirmTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException(
                    $"{nameof(EventBusRabbitMqOptions)}.{nameof(PublishConfirmTimeout)} 必须 > 0,当前={PublishConfirmTimeout}。");
            if (Broker == null)
                throw new InvalidOperationException(
                    $"{nameof(EventBusRabbitMqOptions)}.{nameof(Broker)} 不能为 null。");
            if (Broker.Connection == null)
                throw new InvalidOperationException(
                    $"{nameof(EventBusRabbitMqOptions)}.{nameof(Broker)}.{nameof(RabbitMqOptions.Connection)} 不能为 null。");
            Broker.ValidateNumericLimits();
        }
    }
}
