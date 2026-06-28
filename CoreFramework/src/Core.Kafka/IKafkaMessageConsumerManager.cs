namespace Core.Kafka
{
    /// <summary>
    /// 按 consumer group(group.id)复用 <see cref="IKafkaMessageConsumer"/> 的工厂/注册表。
    /// </summary>
    public interface IKafkaMessageConsumerManager
    {
        /// <summary>
        /// 取或建一个绑定到指定 group 的 consumer。可选传入 <see cref="KafkaTopicDeclareConfigure"/>
        /// 用于显式创建 topic（生产环境推荐），不传则信任 broker 的 auto.create.topics.enable。
        /// </summary>
        IKafkaMessageConsumer TryCreate(string groupId, KafkaTopicDeclareConfigure topicDeclare = null);

        /// <summary>按 group 查询已注册的 consumer;不存在返回 false。</summary>
        bool TryGet(string groupId, out IKafkaMessageConsumer consumer);

        /// <summary>按 group 移除已注册的 consumer 引用(不会主动 Dispose,由调用方负责)。</summary>
        bool TryRemove(string groupId);
    }
}
