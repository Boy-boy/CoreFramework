namespace Core.EventBus.Local
{
    /// <summary>本地(进程内)事件 handler 的调用抽象;与 broker 消费路径的 <see cref="IMessageHandlerInvoker"/> 分离,
    /// 是为了让本地路径<b>复用调用方 scope/UoW</b>,而 broker 路径<b>开新 scope/UoW</b>。</summary>
    /// <remarks>
    /// <para><b>为什么不能共用 broker 的 invoker</b>:</para>
    /// <para>broker 消费端 invoker(如 <c>InboxAwareMessageHandlerInvoker</c>)每次会 <c>CreateAsyncScope()</c>
    /// 新建 DI scope + 新 UoW。这在 broker 消费端是正确的(消息独立投递,无外层事务边界)。</para>
    /// <para>但本地事件大都从外层 UoW 内触发:外层业务 publish → <c>UoW.CommitAsync</c> 排队 → 调 publisher 派发。
    /// 若此时 invoker 再开新 scope:</para>
    /// <list type="number">
    ///   <item><description>新 scope 的 <c>uowMgr.BeginAsync</c> 检测到 ambient UoW → 返回 <c>ChildUnitOfWork</c>
    ///     (DB API 转发到外层 UoW)。</description></item>
    ///   <item><description>handler 内首次解析的 <c>DbContext</c> 来自<b>新 scope</b>,但通过 <c>ChildUoW</c>
    ///     被<b>注册到外层 UoW</b>。</description></item>
    ///   <item><description>handler 返回 → 新 scope 释放 → 该 <c>DbContext</c> 被 Dispose。</description></item>
    ///   <item><description>外层 UoW 后续 <c>SaveChangesAsync</c> 访问已释放的 <c>DbContext</c> → 异常。</description></item>
    /// </list>
    /// <para>这条 bug 在外层 UoW 已持有同一 <c>DbContext</c> 实例时被掩盖,但不可靠。</para>
    /// <para><b>本接口的契约</b>:实现必须<b>复用调用方 scope</b>(通过注入当前 scope 的 <see cref="System.IServiceProvider"/>),
    /// 不开新 scope、不开新 UoW、不查 inbox(本地事件不会重投)。</para>
    /// </remarks>
    public interface ILocalMessageHandlerInvoker : IMessageHandlerInvoker
    {
    }
}
