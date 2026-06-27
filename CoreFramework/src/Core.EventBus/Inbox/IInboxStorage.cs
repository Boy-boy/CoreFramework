using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Inbox
{
    /// <summary>
    /// 消费端幂等存储（inbox / 接收簿）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么需要 inbox</b><br/>
    /// outbox 保证"业务变更 → 事件最终送达 broker"是<i>至少一次</i>语义：
    /// dispatcher 重投、broker 重投、多 dispatcher 实例并发都可能造成同一条消息被
    /// 投递多次。消费端必须有去重机制才能达成"业务上恰好一次"。
    /// </para>
    /// <para>
    /// <b>键的设计</b><br/>
    /// inbox 表主键为 <c>(MessageId, ConsumerGroup)</c> 复合主键：
    /// <list type="bullet">
    ///   <item><description><c>MessageId</c>：消息全局唯一 id（来自 <see cref="Outbox.MessageEnvelope.Id"/>）</description></item>
    ///   <item><description><c>ConsumerGroup</c>：消费方标识，框架默认用 handler 类型全名，
    ///   这样"同一条消息被 N 个 handler 订阅"时每个 handler 各有自己的去重记录</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>典型用法</b><br/>
    /// <c>InboxAwareMessageHandlerInvoker</c> 在每次调用 handler 前后做这样的事：
    /// </para>
    /// <code>
    /// await using var uow = mgr.Begin(transactional: true);
    /// if (!await inbox.TryAcquireAsync(msg.Id, handlerType.FullName, ct))
    /// {
    ///     // 已经处理过 → 跳过 handler，但仍然 commit（让外层 broker ack）
    ///     await uow.CommitAsync();
    ///     return;
    /// }
    /// await handler.HandleAsync(msg);
    /// await uow.CommitAsync();   // inbox 行 + handler 写入业务表 同事务落库
    /// </code>
    /// </remarks>
    public interface IInboxStorage
    {
        /// <summary>
        /// 启动时建表 / 校验 schema。由后台清理服务在启动时调用一次。
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 尝试登记一条接收记录。
        /// </summary>
        /// <param name="messageId">待消费消息的全局 Id（一般来自 <see cref="Outbox.MessageEnvelope.Id"/>）。</param>
        /// <param name="consumerGroup">消费方标识，框架默认传入 handler 的类型全名。</param>
        /// <returns>
        /// <list type="bullet">
        ///   <item><description><c>true</c>：首次接收，调用方应继续执行 handler，并在<b>同一事务</b>中 commit；</description></item>
        ///   <item><description><c>false</c>：之前已处理过，调用方应直接 commit 并 ack broker（跳过 handler）。</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// 该方法只把 inbox 行加入 ChangeTracker / 未提交事务；commit 由调用方所在的 UoW 统一负责。
        /// 这样如果 handler 业务失败，inbox 行也会跟随回滚，下次重投时不会被误判为"已处理"。
        /// </para>
        /// <para>
        /// 主键冲突的兜底：由于"先 Add 再统一 commit"的设计与"立即捕获冲突"互斥
        /// （EfCore 实现选择前者以保证业务行 + inbox 行原子性），主键冲突在 commit 阶段才会暴露为
        /// <c>DbUpdateException</c>，由 <see cref="Core.EventBus.Storage.EfCore.InboxAwareMessageHandlerInvoker"/>
        /// 在 commit 异常路径中识别并视作"另一并发消费已抢先登记"语义。
        /// 实现者扩展其他存储时若选择"立即 INSERT"策略，可在本方法内 catch 主键冲突并返回 false。
        /// </para>
        /// </remarks>
        Task<bool> TryAcquireAsync(Guid messageId, string consumerGroup, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除 <c>ProcessedAtUtc &lt; utcThreshold</c> 的历史记录。
        /// 由 <c>InboxCleanupService</c> 周期调用。
        /// </summary>
        /// <param name="utcThreshold">阈值时刻（UTC），通常为"现在 - RetentionDays"。</param>
        /// <returns>本次清理删除的行数；可用于监控。</returns>
        Task<int> CleanupAsync(DateTime utcThreshold, CancellationToken cancellationToken = default);
    }
}
