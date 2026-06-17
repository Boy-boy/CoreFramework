namespace Core.EventBus.Integration
{
    /// <summary>
    /// 集成事件专属的 handler 注册表抽象。
    /// 单独定义类型让本地与集成事件订阅表在 DI 中互不干扰；语义同 <see cref="IMessageHandlerManager"/>。
    /// </summary>
    public interface IIntegrationMessageHandlerManager : IMessageHandlerManager
    {
    }
}
