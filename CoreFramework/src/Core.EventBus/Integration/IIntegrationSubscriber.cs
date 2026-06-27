namespace Core.EventBus.Integration
{
    /// <summary>跨进程集成事件订阅器抽象;由 broker 模块负责声明基础设施并把回调路由到 <see cref="IMessageHandlerInvoker"/>。</summary>
    public interface IIntegrationSubscriber : IMessageSubscriber
    {
    }
}
