namespace Core.EventBus.Local
{
    /// <summary>
    /// 本地（进程内）事件 publisher 抽象。业务侧统一注入本接口而非具体实现，
    /// 便于单元测试 mock 与未来替换实现。默认实现为 <see cref="LocalMessagePublisher"/>。
    /// </summary>
    public interface ILocalMessagePublisher: IMessagePublisher
    {
    }
}
