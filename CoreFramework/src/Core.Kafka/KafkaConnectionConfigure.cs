using Confluent.Kafka;

namespace Core.Kafka
{
    /// <summary>
    /// Kafka 连接配置。承载 bootstrap server 列表与安全 / 认证选项。
    /// </summary>
    /// <remarks>
    /// 单 broker 与 cluster 模式皆通过 <see cref="BootstrapServers"/> 表达：
    /// 多 broker 用逗号分隔，例如 <c>"kafka-1:9092,kafka-2:9092,kafka-3:9092"</c>。
    /// </remarks>
    public class KafkaConnectionConfigure
    {
        /// <summary>bootstrap server 列表，逗号分隔。例：<c>"kafka:9092"</c>。</summary>
        public string BootstrapServers { get; set; }

        /// <summary>SASL 用户名（PLAIN/SCRAM 机制时使用）；明文 broker 时留空。</summary>
        public string SaslUsername { get; set; }

        /// <summary>SASL 密码。</summary>
        public string SaslPassword { get; set; }

        /// <summary>SASL 机制，常用值：<c>PLAIN</c>、<c>SCRAM-SHA-256</c>、<c>SCRAM-SHA-512</c>。</summary>
        public SaslMechanism? SaslMechanism { get; set; }

        /// <summary>安全协议，明文用 <c>Plaintext</c>；TLS 用 <c>Ssl</c>；SASL+TLS 用 <c>SaslSsl</c>。</summary>
        public SecurityProtocol? SecurityProtocol { get; set; }

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
                    "（或 EventBus:Kafka:Connection:BootstrapServers）节点设置 broker 地址，" +
                    "格式 \"host1:9092,host2:9092\"。");
            }

            var config = new ClientConfig
            {
                BootstrapServers = BootstrapServers,
            };
            if (SecurityProtocol.HasValue)
                config.SecurityProtocol = SecurityProtocol.Value;
            if (SaslMechanism.HasValue)
                config.SaslMechanism = SaslMechanism.Value;
            if (!string.IsNullOrEmpty(SaslUsername))
                config.SaslUsername = SaslUsername;
            if (!string.IsNullOrEmpty(SaslPassword))
                config.SaslPassword = SaslPassword;
            return config;
        }
    }
}
