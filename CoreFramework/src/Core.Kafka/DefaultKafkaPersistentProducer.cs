using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Kafka
{
    /// <summary>
    /// <see cref="IKafkaPersistentProducer"/> 默认实现。
    /// </summary>
    /// <remarks>
    /// <para><b>关键生产者配置</b></para>
    /// <list type="bullet">
    ///   <item><description><c>EnableIdempotence = true</c>：broker 侧基于 producer id + sequence
    ///   去重，避免网络重试导致同一条消息落 broker 两次。这是 outbox 至少一次语义的天然补充。</description></item>
    ///   <item><description><c>Acks = All</c>：等所有 ISR 副本确认才返回，丢消息概率最低（前提是 topic 复制因子 ≥ 2）。</description></item>
    ///   <item><description><c>MessageSendMaxRetries</c>：内置重试，结合 idempotence 不会重复。</description></item>
    /// </list>
    /// </remarks>
    public class DefaultKafkaPersistentProducer : IKafkaPersistentProducer
    {
        private readonly KafkaOptions _options;
        private readonly ILogger<DefaultKafkaPersistentProducer> _logger;
        private readonly object _syncRoot = new();

        /// <summary>
        /// 用 volatile 让 DCLP 里"先读后进锁再检查"的第一次读拿到最新值,不依赖 CLR 强内存模型的隐式保证。
        /// </summary>
        private volatile IProducer<string, byte[]> _producer;
        private bool _disposed;

        public DefaultKafkaPersistentProducer(
            IOptions<KafkaOptions> options,
            ILogger<DefaultKafkaPersistentProducer> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        private IProducer<string, byte[]> Producer
        {
            get
            {
                if (_producer != null) return _producer;
                lock (_syncRoot)
                {
                    if (_producer != null) return _producer;
                    var baseConfig = _options.Connection.BuildClientConfig();
                    var producerConfig = new ProducerConfig(baseConfig)
                    {
                        EnableIdempotence = true,
                        Acks = Acks.All,
                        MessageSendMaxRetries = 5,
                    };

                    _producer = new ProducerBuilder<string, byte[]>(producerConfig)
                        .SetErrorHandler((_, e) =>
                            _logger.LogWarning("Kafka producer error: code={Code} reason={Reason} fatal={Fatal}",
                                e.Code, e.Reason, e.IsFatal))
                        .SetLogHandler((_, m) =>
                            _logger.LogTrace("Kafka producer log: {Facility} {Message}", m.Facility, m.Message))
                        .Build();
                    return _producer;
                }
            }
        }

        public void Produce(string topic, Message<string, byte[]> message)
        {
            Producer.Produce(topic, message, report =>
            {
                if (report.Error.IsError)
                {
                    // 异步回调出错只记日志：业务路径走 ProduceAsync 才能拿到异常
                    _logger.LogError("Kafka delivery failed: topic={Topic} reason={Reason}",
                        report.Topic, report.Error.Reason);
                }
            });
        }

        public Task<DeliveryResult<string, byte[]>> ProduceAsync(string topic, Message<string, byte[]> message,
            CancellationToken cancellationToken = default)
        {
            return Producer.ProduceAsync(topic, message, cancellationToken);
        }

        public int Flush(TimeSpan timeout)
        {
            return _producer?.Flush(timeout) ?? 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                // 给 in-flight 消息一个有限时间送达，避免进程退出时丢
                _producer?.Flush(TimeSpan.FromSeconds(5));
                _producer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka producer dispose failed");
            }
        }
    }
}
