using System;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Core.Kafka
{
    /// <summary>
    /// 单一 consumer group 的 Kafka 消费者抽象。类比 <see cref="IRabbitMqMessageConsumer"/>，
    /// 但语义略有差异：RabbitMQ 维护一个 queue 上的多个 routing key 绑定；
    /// Kafka 维护一个 consumer group 订阅的多个 topic。
    /// </summary>
    /// <remarks>
    /// <para><b>Subscribe 语义</b></para>
    /// <para>
    /// 每次 <see cref="SubscribeTopicAsync"/> / <see cref="UnsubscribeTopicAsync"/> 都会
    /// 触发 group 的 partition 重平衡。批量初始化建议在订阅器启动期一次性完成所有 topic 集合后再启动 poll 循环，
    /// 避免反复 rebalance 拖慢启动。
    /// </para>
    /// </remarks>
    public interface IKafkaMessageConsumer : IDisposable
    {
        /// <summary>把一个 topic 加入本 consumer group 的订阅集合。幂等。</summary>
        Task SubscribeTopicAsync(string topic);

        /// <summary>把一个 topic 从订阅集合中移除。幂等。</summary>
        Task UnsubscribeTopicAsync(string topic);

        /// <summary>是否还有 topic 在订阅。订阅集为空时上层会释放本 consumer。</summary>
        bool HasAnyTopic();

        /// <summary>
        /// 注册消息处理回调。回调签名与 RabbitMQ 实现保持一致风格：
        /// 入参第二个参数是原始消息（<see cref="ConsumeResult{TKey, TValue}"/>），
        /// 处理失败抛异常即可，由 consumer 决定是否 commit。
        /// </summary>
        void OnMessageReceived(Func<IConsumer<string, byte[]>, ConsumeResult<string, byte[]>, Task> processEvent);
    }
}
