using Core.RabbitMQ;
using System;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>RabbitMQ broker 配置;绑定 <c>EventBus:RabbitMq</c> 节点。</summary>
    public class EventBusRabbitMqOptions
    {
        private string _defaultExchangeName = "event_bus_default_routing";

        /// <summary>全局 direct exchange 名,按 <see cref="MessageNameAttribute"/> 作为 routing key 绑定到各 queue;不可为 null。</summary>
        public string ExchangeName
        {
            get => _defaultExchangeName;
            set => _defaultExchangeName = value ?? throw new Exception("exchange is not allowed to be null");
        }

        public EventBusRabbitMqOptions()
        {
            Connection = new RabbitMqConnectionConfigure();
        }

        /// <summary>RabbitMQ 连接参数(host / port / vhost / 凭据等)。</summary>
        public RabbitMqConnectionConfigure Connection { get; set; }

        /// <summary>handler 失败时的 ack/nack 策略;默认 <see cref="RabbitMqFailureBehavior.RequeueOnce"/> 配合 inbox 去重达成最终一致。</summary>
        /// <remarks>选 <see cref="RabbitMqFailureBehavior.NackNoRequeue"/> 时建议配置 DLX,否则二次失败消息会被 broker 直接丢弃。</remarks>
        public RabbitMqFailureBehavior FailureBehavior { get; set; } = RabbitMqFailureBehavior.RequeueOnce;

        /// <summary>死信交换机名,可选;未配置时非重投路径的失败消息会被丢弃,启动时会 LogWarning。</summary>
        public string DeadLetterExchange { get; set; }

        /// <summary>死信 routing key,可选。</summary>
        public string DeadLetterRoutingKey { get; set; }

        /// <summary>publisher channel 池上限,即同时刻最多并发 publish 数;默认 8。</summary>
        /// <remarks>channel 首次创建时一次性完成 ExchangeDeclare + ConfirmSelect + BasicReturn 挂载,后续 publish 复用走纯 BasicPublish + WaitForConfirms;超出本值的并发请求由 SemaphoreSlim 排队。</remarks>
        public int ChannelPoolSize { get; set; } = 8;

        /// <summary>反序列化失败 / Id 校验失败的"毒消息"处置策略;默认 <see cref="PoisonMessageBehavior.SkipAndAck"/>。</summary>
        /// <remarks>需要 DLX 收集毒消息时改为 <see cref="PoisonMessageBehavior.ThrowAndLetBrokerHandle"/> 并配合 <c>FailureBehavior=NackNoRequeue</c> + <c>DeadLetterExchange</c>。</remarks>
        public PoisonMessageBehavior PoisonMessageBehavior { get; set; } = PoisonMessageBehavior.SkipAndAck;
    }
}
