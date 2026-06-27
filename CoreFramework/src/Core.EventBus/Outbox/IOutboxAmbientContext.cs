namespace Core.EventBus.Outbox
{
    /// <summary>"当前是否处于 outbox 上下文"的运行时信号;publisher 据此选 outbox/直发。</summary>
    /// <remarks>
    /// 抽出此接口而不直接依赖 <c>IUnitOfWorkAccessor</c>:<c>Core.Uow</c> 已依赖 <c>Core.EventBus</c>,反向引用会形成循环。
    /// 由 <c>Core.Uow</c> 内部实现并注册到 DI;未来引入非 UoW 事务边界时也只需新增实现,publisher 不必改。
    /// </remarks>
    public interface IOutboxAmbientContext
    {
        /// <summary>当前是否存在可承载 outbox 写入的事务上下文。</summary>
        /// <remarks>true → 走 outbox(随业务事务落库);false → 直发 broker(best-effort)。</remarks>
        bool IsActive { get; }
    }
}
