namespace Core.EventBus.Integration
{
    /// <summary>
    /// 跨进程集成事件 publisher 抽象。业务侧注入本接口，
    /// 实际实现由具体 broker 模块提供（如 RabbitMQ 实现为 <c>RabbitMqMessagePublisher</c>）。
    /// </summary>
    /// <remarks>
    /// PublishAsync 的事务一致性语义由 <see cref="IntegrationMessagePublisherBase"/> 接管：
    /// 检测到 outbox 上下文时把消息持久化到 outbox 表，否则直发 broker。
    /// </remarks>
    public interface IIntegrationMessagePublisher : IMessagePublisher
    {
    }
}
