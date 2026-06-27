namespace Core.EventBus.Integration
{
    /// <summary>集成事件专属 handler 注册表;与本地事件订阅表在 DI 中类型隔离。</summary>
    public interface IIntegrationMessageHandlerManager : IMessageHandlerManager
    {
    }
}
