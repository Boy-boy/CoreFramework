namespace Core.EventBus.Local
{
    /// <summary>
    /// 本地(进程内)事件 handler 的调用抽象;与 broker 消费路径的 <see cref="IMessageHandlerInvoker"/> 分离,
    /// 让本地路径<b>复用调用方 scope/UoW</b>,而 broker 路径<b>开新 scope/UoW</b>。
    /// </summary>
    /// <remarks>
    /// <para><b>常见误区</b>:想"把 <see cref="IMessageHandlerInvoker"/> 改 Scoped、删掉本接口让 local 复用"
    /// —— 拦不住的是<b>实现层语义</b>,不是<b>生命周期</b>。下面 4 个维度 local 与 broker 全相反:</para>
    /// <list type="bullet">
    ///   <item><description><b>DI scope</b>:local 必须共享调用方 scope;broker 必须新建 scope(消息独立)</description></item>
    ///   <item><description><b>UoW</b>:local 共享外层业务 UoW(handler 写入与业务原子提交);broker 新开 transactional UoW</description></item>
    ///   <item><description><b>inbox</b>:local 不查(本地事件不会重投);broker 必查(broker 至少一次 → 业务恰好一次)</description></item>
    ///   <item><description><b>失败语义</b>:local 抛错让外层业务事务回滚;broker 抛错让消息不 ack 待 broker 重投</description></item>
    /// </list>
    ///
    /// <para><b>为什么仅靠改 Scoped 统一不可行</b>:</para>
    /// <list type="number">
    ///   <item><description>broker subscriber 是 Singleton(管 broker 长连接),不能注入 Scoped invoker
    ///     (捕获 Scoped 反模式,DI 容器会拒绝)</description></item>
    ///   <item><description>broker 实现要 <c>CreateAsyncScope()</c> 隔离每条消息,这步在 local 路径下会触发
    ///     下面那条 ChildUnitOfWork + DbContext 释放 bug</description></item>
    ///   <item><description>若改"看到 ambient UoW 就走 local 路径"的运行时分支:外层 hosted service /
    ///     批处理脚本里的 broker 消费会被错认为 local 而跳过 inbox 去重,重投时静默重复处理</description></item>
    /// </list>
    /// <para>结论:两条路径用<b>不同实现 + 不同接口</b>,在编译期把"调用谁"固化下来,
    /// 比运行时启发式检测更可靠。<see cref="IMessageHandlerInvoker"/> 用 Singleton 只是因为 broker invoker 没有
    /// per-call 状态;<see cref="ILocalMessageHandlerInvoker"/> 用 Scoped 才是<b>必须</b>的(为了拿到调用方 SP)。</para>
    ///
    /// <para><b>本接口的契约</b>:实现必须<b>复用调用方 scope</b>(通过注入当前 scope 的 <see cref="System.IServiceProvider"/>),
    /// 不开新 scope、不开新 UoW、不查 inbox。</para>
    ///
    /// <para><b>底层 bug:broker invoker 在外层 UoW 内开新 scope 会发生什么</b></para>
    /// <list type="number">
    ///   <item><description>新 scope 的 <c>uowMgr.BeginAsync</c> 检测到 ambient UoW → 返回 <c>ChildUnitOfWork</c>
    ///     (DB API 转发到外层 UoW)</description></item>
    ///   <item><description>handler 内首次解析的 <c>DbContext</c> 来自<b>新 scope</b>,但通过 <c>ChildUoW</c>
    ///     被<b>注册到外层 UoW</b></description></item>
    ///   <item><description>handler 返回 → 新 scope 释放 → 该 <c>DbContext</c> 被 Dispose</description></item>
    ///   <item><description>外层 UoW 后续 <c>SaveChangesAsync</c> 访问已释放的 <c>DbContext</c> → 异常</description></item>
    /// </list>
    /// <para>这条 bug 在外层 UoW 已持有同一 <c>DbContext</c> 实例时被掩盖,但不可靠。</para>
    /// </remarks>
    public interface ILocalMessageHandlerInvoker : IMessageHandlerInvoker
    {
    }
}
