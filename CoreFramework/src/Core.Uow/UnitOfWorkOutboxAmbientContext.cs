using Core.EventBus.Outbox;

namespace Core.Uow
{
    /// <summary>
    /// <see cref="IOutboxAmbientContext"/> 在 Core.Uow 侧的实现：把"当前是否有活跃 UoW"
    /// 翻译为"是否处于 outbox 上下文"。
    /// </summary>
    /// <remarks>
    /// <para><b>解循环依赖</b></para>
    /// <para>
    /// <c>Core.Uow</c> 项目本来就依赖 <c>Core.EventBus</c>（<see cref="IMessage"/> 等基础契约），
    /// 反向依赖会形成项目级循环。让 <c>Core.EventBus</c> 只声明 <see cref="IOutboxAmbientContext"/> 接口、
    /// 由 <c>Core.Uow</c> 注入实现，是单向解耦的标准做法。
    /// </para>
    ///
    /// <para><b>生命周期</b></para>
    /// <para>
    /// 注册为 Transient，每次注入都从 Singleton 的 <see cref="IUnitOfWorkAccessor"/> 读取
    /// 当前 AsyncLocal 中的 UoW。Singleton 与 AsyncLocal 不冲突 —— AsyncLocal 内部值
    /// 才是"按调用上下文"分隔的。
    /// </para>
    /// </remarks>
    internal sealed class UnitOfWorkOutboxAmbientContext : IOutboxAmbientContext
    {
        private readonly IUnitOfWorkAccessor _accessor;

        public UnitOfWorkOutboxAmbientContext(IUnitOfWorkAccessor accessor)
        {
            _accessor = accessor;
        }

        /// <summary>
        /// 当前 AsyncLocal 中存在 UoW（且尚未释放）即视为"outbox 上下文活跃"。
        /// publisher 据此决定走 outbox 还是直发。
        /// </summary>
        public bool IsActive => _accessor.UnitOfWork != null;
    }
}
