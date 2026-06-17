namespace Core.EventBus.Integration
{
    /// <summary>
    /// 跨进程集成事件订阅器抽象。具体 broker 模块实现负责声明
    /// exchange / queue / binding 并把消息回调路由到 <see cref="IMessageHandlerInvoker"/>。
    /// </summary>
    public interface IIntegrationMessageSubscribe : IMessageSubscribe
    {
    }
}
