namespace Core.EventBus.Local
{
    /// <summary>
    /// 本地事件订阅器抽象。由 <see cref="EventBusBackgroundService"/> 在启动时调用 Initialize
    /// 把 (messageType, handlerType) 推入；默认实现为 <see cref="LocalMessageSubscriber"/>。
    /// </summary>
    public interface ILocalSubscriber : IMessageSubscriber
    {
    }
}
