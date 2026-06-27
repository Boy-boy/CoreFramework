namespace Core.EventBus.Integration
{
    /// <summary>跨进程集成事件 publisher 抽象;实现由具体 broker 模块提供。</summary>
    /// <remarks>事务一致性由 <see cref="IntegrationMessagePublisherBase"/> 接管:有 outbox 上下文则写入 outbox,否则直发。</remarks>
    public interface IIntegrationPublisher : IMessagePublisher
    {
    }
}
