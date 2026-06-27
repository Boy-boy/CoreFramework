namespace Core.Kafka
{
    /// <summary>
    /// 按 consumer group 复用 <see cref="IKafkaMessageConsumer"/>。
    /// 类比 <see cref="IRabbitMqMessageConsumerManager"/>，但 key 只用 group.id —— Kafka 没有 exchange 概念。
    /// </summary>
    public interface IKafkaMessageConsumerManager
    {
        /// <summary>
        /// 取或建一个绑定到指定 group 的 consumer。可选传入 <see cref="KafkaTopicDeclareConfigure"/>
        /// 用于显式创建 topic（生产环境推荐），不传则信任 broker 的 auto.create.topics.enable。
        /// </summary>
        IKafkaMessageConsumer TryCreate(string groupId, KafkaTopicDeclareConfigure topicDeclare = null);

        bool TryGet(string groupId, out IKafkaMessageConsumer consumer);

        bool TryRemove(string groupId);
    }
}
