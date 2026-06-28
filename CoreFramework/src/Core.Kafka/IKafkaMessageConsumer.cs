using System;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Core.Kafka
{
    /// <summary>
    /// 单一 consumer group 的 Kafka 消费者抽象;一个实例维护一个 group.id 下订阅的多个 topic 集合。
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
        /// 注册消息处理回调。入参为 <see cref="IConsumer{TKey,TValue}"/> 与原始 <see cref="ConsumeResult{TKey, TValue}"/>;
        /// 处理失败抛异常即可,consumer 内部按失败策略 Seek 回 offset 让 broker 重投同条。
        /// </summary>
        void OnMessageReceived(Func<IConsumer<string, byte[]>, ConsumeResult<string, byte[]>, Task> processEvent);
    }
}
