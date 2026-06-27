namespace Core.EventBus.Local
{
    /// <summary>本地事件专属 handler 注册表;与集成事件订阅表在 DI 中类型隔离。</summary>
    public interface ILocalMessageHandlerManager : IMessageHandlerManager
    {
    }
}
