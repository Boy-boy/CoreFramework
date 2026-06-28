using System.Collections.Generic;

namespace Core.Kafka
{
    /// <summary>
    /// Topic 声明配置;承载 topic 名 + partition 数 + 副本因子 + topic 级 broker 配置。
    /// </summary>
    /// <remarks>
    /// <para><b>使用场景</b></para>
    /// <list type="bullet">
    ///   <item><description>开发 / 测试：依赖 broker 的 <c>auto.create.topics.enable</c>，可不显式声明。</description></item>
    ///   <item><description>生产：通过 <see cref="Confluent.Kafka.Admin.IAdminClient"/> 显式建 topic，
    ///   精确控制 partition / replication factor。</description></item>
    /// </list>
    /// </remarks>
    public class KafkaTopicDeclareConfigure
    {
        public string TopicName { get; }

        /// <summary>partition 数。决定并发上限（一个 partition 最多一个 consumer 实例处理）。</summary>
        public int NumPartitions { get; set; }

        /// <summary>副本因子。生产至少 2，配合 <c>Acks=All</c> 保证落盘安全。</summary>
        public short ReplicationFactor { get; set; }

        /// <summary>topic 级 broker 配置（如 <c>retention.ms</c>、<c>cleanup.policy</c>）。</summary>
        public IDictionary<string, string> Configs { get; }

        public KafkaTopicDeclareConfigure(string topicName,
            int numPartitions = 3,
            short replicationFactor = 1,
            Dictionary<string, string> configs = null)
        {
            TopicName = topicName;
            NumPartitions = numPartitions;
            ReplicationFactor = replicationFactor;
            Configs = configs ?? new Dictionary<string, string>();
        }
    }
}
