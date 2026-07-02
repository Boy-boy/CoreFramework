using System.Collections.Generic;
using Confluent.Kafka;

namespace Core.Kafka
{
    /// <summary>
    /// Kafka 连接配置。承载 bootstrap server 列表与 librdkafka 原生 client 配置。
    /// </summary>
    /// <remarks>
    /// 单 broker 与 cluster 模式皆通过 <see cref="BootstrapServers"/> 表达：
    /// 多 broker 用逗号分隔，例如 <c>"kafka-1:9092,kafka-2:9092,kafka-3:9092"</c>。
    /// </remarks>
    public class KafkaConnectionConfigure
    {
        /// <summary>bootstrap server 列表，逗号分隔。例：<c>"kafka:9092"</c>。</summary>
        public string BootstrapServers { get; set; }

        /// <summary>
        /// librdkafka 原生 client 配置。key 使用原生配置名,如 <c>security.protocol</c>、<c>sasl.mechanism</c>。
        /// </summary>
        public Dictionary<string, string> MainConfig { get; set; } = new();

        /// <summary>
        /// 构造一份共享的 <see cref="ClientConfig"/>。Producer / Consumer 在此基础上叠加自己的专属配置。
        /// </summary>
        public virtual ClientConfig BuildClientConfig()
        {
            // 配置缺失时给可读异常，避免让用户在 librdkafka 的 ConfigException 里猜哪里配错了
            if (string.IsNullOrWhiteSpace(BootstrapServers))
            {
                throw new System.InvalidOperationException(
                    "Kafka BootstrapServers 未配置。请在 appsettings.json 的 Kafka:Connection:BootstrapServers" +
                    "（或 EventBus:Kafka:Broker:Connection:BootstrapServers）节点设置 broker 地址，" +
                    "格式 \"host1:9092,host2:9092\"。");
            }

            var config = MainConfig == null
                ? new ClientConfig()
                : new ClientConfig(new Dictionary<string, string>(MainConfig));
            config.BootstrapServers = BootstrapServers;
            return config;
        }
    }
}
