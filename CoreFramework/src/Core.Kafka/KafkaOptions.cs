using System;

namespace Core.Kafka
{
    /// <summary>
    /// Kafka 基础设施层的根配置;由
    /// <see cref="Microsoft.Extensions.DependencyInjection.KafkaServiceCollectionExtensions"/> 绑到 IOptions 系统。
    /// </summary>
    public class KafkaOptions
    {
        public KafkaOptions()
        {
            Connection = new KafkaConnectionConfigure();
        }

        /// <summary>Kafka 连接配置（brokers、SASL、TLS）。</summary>
        public KafkaConnectionConfigure Connection { get; set; }

        /// <summary>
        /// 消费失败时 <see cref="Confluent.Kafka.IConsumer{TKey,TValue}.Seek"/> 回失败 offset 重投前的退避,
        /// 避免 poison message 在 PollLoop 上热循环。默认 5 秒。
        /// </summary>
        /// <remarks>
        /// 与 inbox 去重协同:成功 handler 在 inbox 命中后跳过,失败 handler 借由重投不断重试。
        /// poison message 的最终截断口是 <see cref="MaxConsecutiveFailures"/>;再精细的"丢消息可观测"
        /// (如转 dead-letter topic) 由业务层在 inbox 上跟踪计数后自行实现。
        /// </remarks>
        public TimeSpan FailureBackoff { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 同条 offset 连续失败的最大重试次数。达到上限后框架 commit + LogWarning 跳过该条消息,
        /// 避免 poison message 永久阻塞整个 partition。默认 5 次;设为 0 关闭(无限重试,旧版语义)。
        /// </summary>
        /// <remarks>
        /// 命中上限后框架只 log + commit,不会自动发到 dead-letter topic —— 想要"可观测的丢失"
        /// 请用监控/告警订阅 LogWarning,或在 handler 内基于 inbox 失败计数显式投递 DLT 后吞掉异常
        /// (让本条算作成功 commit)。
        /// </remarks>
        public int MaxConsecutiveFailures { get; set; } = 5;
    }
}
