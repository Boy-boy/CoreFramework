using System;
using Core.Kafka;

namespace Core.EventBus.Kafka
{
    /// <summary>
    /// Kafka broker 模块配置;在 <c>EventBus:Kafka</c> 节点定义,启动期由
    /// <see cref="EventBusOptionsExtensions"/> 绑定到 IOptions。
    /// </summary>
    /// <remarks>
    /// 连接 / 失败 backoff / 重试上限一组字段嵌套在 <see cref="Broker"/> 子对象里(<see cref="KafkaOptions"/> 原样复用),
    /// 启动时 EventBus 层把 <see cref="Broker"/> 通过 <see cref="KafkaOptions.CopyFrom"/> 反射拷贝到
    /// <c>IOptions&lt;KafkaOptions&gt;</c>;这样底层 <see cref="KafkaOptions"/> 新增可写属性会自动透传,
    /// EventBus 层无需跟改、appsettings 也无须写两遍。
    /// </remarks>
    public class EventBusKafkaOptions
    {
        public EventBusKafkaOptions()
        {
            Broker = new KafkaOptions();
            DefaultPartitionCount = 3;
            DefaultReplicationFactor = 1;
        }

        /// <summary>底层 Kafka 模块配置(连接 / FailureBackoff / MaxConsecutiveFailures);绑定 <c>EventBus:Kafka:Broker</c> 节点。</summary>
        /// <remarks>EventBus 层不再单独声明 Connection / FailureBackoff / MaxConsecutiveFailures 字段,统一以 <see cref="KafkaOptions"/> 原型为准 —— 底层新增字段会自动透传到 <c>IOptions&lt;KafkaOptions&gt;</c>。</remarks>
        public KafkaOptions Broker { get; set; }

        /// <summary>所有 topic 的全局前缀,便于多租户/多环境共享 broker。</summary>
        public string TopicPrefix { get; set; }

        /// <summary>显式建 topic 时的 partition 数,3 适合多数业务。</summary>
        public int DefaultPartitionCount { get; set; }

        /// <summary>显式建 topic 时的副本因子;生产建议 ≥ 2。</summary>
        public short DefaultReplicationFactor { get; set; }

        /// <summary>是否在订阅端启动时显式建 topic;false 时信任 broker 的 <c>auto.create.topics.enable</c>。</summary>
        public bool DeclareTopicsOnSubscribe { get; set; } = false;

        /// <summary>启动期校验,数值非法立刻抛 <see cref="InvalidOperationException"/>。</summary>
        public void Validate()
        {
            if (Broker == null)
                throw new InvalidOperationException(
                    $"{nameof(EventBusKafkaOptions)}.{nameof(Broker)} 不能为 null。");
            if (Broker.Connection == null)
                throw new InvalidOperationException(
                    $"{nameof(EventBusKafkaOptions)}.{nameof(Broker)}.{nameof(KafkaOptions.Connection)} 不能为 null。");
            if (DefaultPartitionCount <= 0)
                throw new InvalidOperationException(
                    $"{nameof(EventBusKafkaOptions)}.{nameof(DefaultPartitionCount)} 必须 > 0,当前={DefaultPartitionCount}。");
            if (DefaultReplicationFactor <= 0)
                throw new InvalidOperationException(
                    $"{nameof(EventBusKafkaOptions)}.{nameof(DefaultReplicationFactor)} 必须 > 0,当前={DefaultReplicationFactor}。");
            if (Broker.FailureBackoff < TimeSpan.Zero)
                throw new InvalidOperationException(
                    $"{nameof(EventBusKafkaOptions)}.{nameof(Broker)}.{nameof(KafkaOptions.FailureBackoff)} 不能为负,当前={Broker.FailureBackoff}。");
            if (Broker.MaxConsecutiveFailures < 0)
                throw new InvalidOperationException(
                    $"{nameof(EventBusKafkaOptions)}.{nameof(Broker)}.{nameof(KafkaOptions.MaxConsecutiveFailures)} 不能为负数(0 表示无限重试),当前={Broker.MaxConsecutiveFailures}。");
        }
    }
}
