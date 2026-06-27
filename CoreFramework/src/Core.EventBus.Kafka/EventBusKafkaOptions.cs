using System;
using Core.Kafka;

namespace Core.EventBus.Kafka
{
    /// <summary>
    /// Kafka broker 模块配置。在 appsettings.json 的 <c>EventBus:Kafka</c> 节点定义，
    /// 启动期由 <see cref="EventBusOptionsExtensions"/> 绑定到 IOptions 系统。
    /// </summary>
    /// <remarks>
    /// <para><b>与 RabbitMQ 选项的语义差异</b></para>
    /// <list type="bullet">
    ///   <item><description>RabbitMQ 用一个共享 exchange + routing key 寻址；Kafka 用 topic 寻址，
    ///   故无 <c>ExchangeName</c>，但允许通过 <see cref="TopicPrefix"/> 给所有 topic 加前缀
    ///   （便于多租户 / 多环境共享 broker）。</description></item>
    ///   <item><description><see cref="DefaultPartitionCount"/> / <see cref="DefaultReplicationFactor"/>：
    ///   显式声明 topic 时使用，未显式声明则信任 broker 配置。</description></item>
    /// </list>
    /// </remarks>
    public class EventBusKafkaOptions
    {
        public EventBusKafkaOptions()
        {
            Connection = new KafkaConnectionConfigure();
            DefaultPartitionCount = 3;
            DefaultReplicationFactor = 1;
        }

        /// <summary>Kafka 连接配置（brokers、SASL、TLS）。</summary>
        public KafkaConnectionConfigure Connection { get; set; }

        /// <summary>
        /// 所有 topic 的全局前缀。例如设为 <c>"prod."</c> 时，事件名 <c>order.created</c>
        /// 实际 topic 为 <c>"prod.order.created"</c>。生产端 / 消费端都按这个前缀拼。
        /// </summary>
        public string TopicPrefix { get; set; }

        /// <summary>
        /// 显式建 topic 时的 partition 数；订阅端发起 topic 声明时使用。3 是一个适合多数业务的默认值。
        /// </summary>
        public int DefaultPartitionCount { get; set; }

        /// <summary>
        /// 显式建 topic 时的副本因子。开发环境默认 1，生产建议在配置里调到 ≥ 2。
        /// </summary>
        public short DefaultReplicationFactor { get; set; }

        /// <summary>
        /// 是否在订阅端启动时显式调用 AdminClient 建 topic。
        /// false 时完全信任 broker 的 <c>auto.create.topics.enable</c>。
        /// </summary>
        public bool DeclareTopicsOnSubscribe { get; set; } = false;

        /// <summary>
        /// 消费失败时 PollLoop Seek 回 offset 重投前的退避，默认 5 秒。
        /// 由 <see cref="EventBusOptionsExtensions.AddServices"/> 桥接到底层 <see cref="KafkaOptions.FailureBackoff"/>。
        /// </summary>
        /// <remarks>
        /// poison message 的最终截断口是 <see cref="MaxConsecutiveFailures"/>;
        /// 长期失败再精细的"可观测丢失"请在 handler 内显式落 dead-letter topic 并吞掉异常,
        /// 让 PollLoop 视作成功 commit。
        /// </remarks>
        public TimeSpan FailureBackoff { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 同条 offset 连续失败的最大重试次数,达到上限后框架跳过该条消息推进 offset。默认 5;
        /// 设为 0 关闭(无限重试)。详见 <see cref="KafkaOptions.MaxConsecutiveFailures"/>。
        /// </summary>
        public int MaxConsecutiveFailures { get; set; } = 5;
    }
}
