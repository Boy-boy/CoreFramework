using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Core.Kafka
{
    /// <summary>
    /// Kafka 持久化生产者抽象;在整个进程内维护一个长生命周期的
    /// <see cref="IProducer{TKey,TValue}"/> 实例并复用。
    /// </summary>
    /// <remarks>
    /// <para><b>为什么是 Singleton</b></para>
    /// <para>
    /// Confluent.Kafka 的 producer 内部维护 broker 连接池、批处理缓冲、压缩线程，
    /// 反复 create/dispose 代价非常大，且会造成 metadata 抖动。框架统一持有一个 Singleton 实例，
    /// 业务侧通过 <see cref="ProduceAsync"/> 共享。
    /// </para>
    /// </remarks>
    public interface IKafkaPersistentProducer : IDisposable
    {
        /// <summary>同步投递一条消息（fire-and-forget），由内部缓冲异步落 broker。</summary>
        void Produce(string topic, Message<string, byte[]> message);

        /// <summary>异步投递一条消息，await 之后保证 broker 已 ack（按 <c>Acks</c> 配置）。</summary>
        Task<DeliveryResult<string, byte[]>> ProduceAsync(string topic, Message<string, byte[]> message,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 刷新本地缓冲，直到所有 in-flight 消息被 broker ack 或超时。
        /// 用于关键路径上保证语义"已送达"。
        /// </summary>
        int Flush(TimeSpan timeout);
    }
}
