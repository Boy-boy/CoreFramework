namespace Core.EventBus.Outbox
{
    /// <summary>
    /// "当前是否处于 outbox 上下文"的运行时信号。
    /// <see cref="Integration.IntegrationMessagePublisherBase.PublishAsync{T}"/> 通过查询此接口
    /// 决定走 outbox 路径还是直发路径。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么不直接依赖 IUnitOfWorkAccessor？</b><br/>
    /// 因为 <c>Core.Uow</c> 已经依赖 <c>Core.EventBus</c>（UoW 需要 <see cref="IMessage"/> 来管理事件），
    /// 如果 <c>Core.EventBus</c> 反过来直接引用 <c>Core.Uow</c> 就会形成循环。
    /// 这里抽出一个仅声明在 <c>Core.EventBus</c> 的接口，由 <c>Core.Uow</c> 内部实现并注册到 DI，
    /// 把"上下文判定"这个语义解耦出来。
    /// </para>
    /// <para>
    /// <b>实现示例</b>（位于 <c>Core.Uow.UnitOfWorkOutboxAmbientContext</c>）：
    /// <code>
    /// public bool IsActive =&gt; _unitOfWorkAccessor.UnitOfWork != null;
    /// </code>
    /// </para>
    /// <para>
    /// <b>未来扩展</b>：如果项目引入非 UoW 的事务边界（如 EF Core <c>SaveChanges</c>
    /// 拦截器、TransactionScope 等），可以加新的实现承担此信号；publisher 不需要改动。
    /// </para>
    /// </remarks>
    public interface IOutboxAmbientContext
    {
        /// <summary>
        /// 当前是否存在可承载 outbox 写入的事务上下文。
        /// </summary>
        /// <remarks>
        /// true → publisher 走 outbox（事件随业务事务落库）；<br/>
        /// false → publisher 直发到 broker（best-effort，无业务一致性保证）。
        /// </remarks>
        bool IsActive { get; }
    }
}
