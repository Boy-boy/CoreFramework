namespace Core.EventBus.Local
{
    /// <summary>
    /// 本地事件专属的 handler 注册表抽象。
    /// 单独定义一份接口（而不复用 <see cref="IMessageHandlerManager"/>）的目的：
    /// 让本地与集成事件各自有独立的订阅表，避免互相污染。
    /// </summary>
    public interface ILocalMessageHandlerManager : IMessageHandlerManager
    {
    }
}
