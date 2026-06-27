using Core.RabbitMQ;
using System;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>
    /// RabbitMQ broker 模块的配置。在 appsettings.json 的 <c>EventBus:RabbitMq</c> 节点定义，
    /// 启动期由 <see cref="EventBusOptionsExtensions"/> 绑定到 IOptions 系统。
    /// </summary>
    /// <remarks>
    /// 配置项的语义：
    /// <list type="bullet">
    ///   <item><description><see cref="ExchangeName"/>：所有事件共用同一个 direct exchange，
    ///   按 routing key（即 <see cref="MessageNameAttribute"/> 值）路由到不同 queue。</description></item>
    ///   <item><description><see cref="Connection"/>：RabbitMQ 连接参数（host / port / 凭据）。</description></item>
    /// </list>
    /// </remarks>
    public class EventBusRabbitMqOptions
    {
        private string _defaultExchangeName = "event_bus_default_routing";

        /// <summary>
        /// 全局 exchange 名（direct 类型）。所有事件按 <see cref="MessageNameAttribute"/>
        /// 作为 routing key 绑定到这个 exchange。不允许为 null。
        /// </summary>
        public string ExchangeName
        {
            get => _defaultExchangeName;
            set => _defaultExchangeName = value ?? throw new Exception("exchange is not allowed to be null");
        }

        public EventBusRabbitMqOptions()
        {
            Connection = new RabbitMqConnectionConfigure();
        }

        /// <summary>RabbitMQ 连接配置（host、port、vhost、凭据等）。</summary>
        public RabbitMqConnectionConfigure Connection { get; set; }

        /// <summary>
        /// handler 失败时的 ack/nack 策略。默认 <see cref="RabbitMqFailureBehavior.RequeueOnce"/>,
        /// 让 broker 在首次失败时重投一次,与上层 inbox 去重协同达成业务最终一致性。
        /// 由 <see cref="EventBusOptionsExtensions.AddServices"/> 桥接到底层 <see cref="RabbitMqOptions.FailureBehavior"/>。
        /// </summary>
        /// <remarks>
        /// 选 <see cref="RabbitMqFailureBehavior.NackNoRequeue"/> 时强烈建议在 queue declare 上配置 DLX,
        /// 否则二次失败的消息会被 broker 直接丢弃。
        /// </remarks>
        public RabbitMqFailureBehavior FailureBehavior { get; set; } = RabbitMqFailureBehavior.RequeueOnce;

        /// <summary>
        /// 死信交换机名,可选。详见 <see cref="RabbitMqOptions.DeadLetterExchange"/>。
        /// 不设置时,失败消息在非重投路径会直接丢弃 —— 启动时会有 LogWarning 提醒。
        /// </summary>
        public string DeadLetterExchange { get; set; }

        /// <summary>
        /// 死信 routing key,可选。详见 <see cref="RabbitMqOptions.DeadLetterRoutingKey"/>。
        /// </summary>
        public string DeadLetterRoutingKey { get; set; }

        /// <summary>
        /// publisher 端 channel 池的上限,即"同一时刻最多有多少个 publish 并发"。
        /// 默认 8 兼顾常见高并发场景与 broker 端 channel 计数的克制。
        /// </summary>
        /// <remarks>
        /// 每条 channel 在首次创建时一次性完成 ExchangeDeclare + ConfirmSelect + 挂 BasicReturn 监听,
        /// 后续 publish 复用同一 channel 只做纯 BasicPublish + WaitForConfirms。
        /// 同时持有 channel 的线程数受 SemaphoreSlim 限制为本值;池外的并发请求会排队等待。
        /// </remarks>
        public int ChannelPoolSize { get; set; } = 8;
    }
}
