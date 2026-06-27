namespace Core.EventBus.Local
{
    /// <summary>本地(进程内)事件 publisher 抽象;默认实现 <see cref="LocalMessagePublisher"/>。</summary>
    public interface ILocalPublisher : IMessagePublisher
    {
    }
}
