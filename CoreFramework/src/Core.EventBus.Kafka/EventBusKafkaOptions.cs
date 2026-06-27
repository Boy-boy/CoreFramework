using System;
using Core.Kafka;

namespace Core.EventBus.Kafka
{
    /// <summary>
    /// Kafka broker 模块配置;在 <c>EventBus:Kafka</c> 节点定义,启动期由
    /// <see cref="EventBusOptionsExtensions"/> 绑定到 IOptions。
    /// </summary>
    public class EventBusKafkaOptions
    {
        public EventBusKafkaOptions()
        {
            Connection = new KafkaConnectionConfigure();
            DefaultPartitionCount = 3;
            DefaultReplicationFactor = 1;
        }

        /// <summary>Kafka 连接配置(brokers、SASL、TLS)。</summary>
        public KafkaConnectionConfigure Connection { get; set; }

        /// <summary>所有 topic 的全局前缀,便于多租户/多环境共享 broker。</summary>
        public string TopicPrefix { get; set; }

        /// <summary>显式建 topic 时的 partition 数,3 适合多数业务。</summary>
        public int DefaultPartitionCount { get; set; }

        /// <summary>显式建 topic 时的副本因子;生产建议 ≥ 2。</summary>
        public short DefaultReplicationFactor { get; set; }

        /// <summary>是否在订阅端启动时显式建 topic;false 时信任 broker 的 <c>auto.create.topics.enable</c>。</summary>
        public bool DeclareTopicsOnSubscribe { get; set; } = false;

        /// <summary>消费失败后 Seek 回 offset 重投前的退避,默认 5s。</summary>
        /// <remarks>
        /// poison message 的最终截断口是 <see cref="MaxConsecutiveFailures"/>;
        /// 长期失败请在 handler 内显式落 dead-letter topic 并吞掉异常。
        /// </remarks>
        public TimeSpan FailureBackoff { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>同条 offset 连续失败的最大重试次数,达上限后跳过推进 offset。默认 5;0 表示无限重试。</summary>
        public int MaxConsecutiveFailures { get; set; } = 5;
    }
}
