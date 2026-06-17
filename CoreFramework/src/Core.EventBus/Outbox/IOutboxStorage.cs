using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Outbox
{
    /// <summary>
    /// Outbox（发件箱）持久化抽象，是事件总线"事务一致性"语义的核心承载。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 设计目标：解决"业务写库成功但事件没发出去 / 事件发出去但业务回滚"的双写问题。
    /// 实现思路：把"待投递的消息"作为一行普通的业务数据，和业务变更落在 <b>同一个数据库事务</b> 内提交，
    /// 由独立的后台 dispatcher 周期扫描这张表把消息推送到 broker（RabbitMQ 等）。
    /// </para>
    ///
    /// <para>典型调用时序：</para>
    /// <list type="number">
    ///   <item><description>生产者：业务事务中 → <see cref="StoreMessageAsync"/> → 业务 commit（含 outbox 行）</description></item>
    ///   <item><description>Dispatcher：<see cref="FetchReadyAsync"/> → 调用 broker → 成功 <see cref="DeleteAsync"/> / 失败 <see cref="MarkFailedAsync"/></description></item>
    ///   <item><description>Dispatcher：超过重试上限 → <see cref="MoveToDeadLetterAsync"/></description></item>
    /// </list>
    ///
    /// <para>实现者注意：</para>
    /// <list type="bullet">
    ///   <item><description>生产者侧的"写入"必须与业务变更共享同一事务（典型：让生产者用业务 DbContext 把 outbox 实体 Add 到 ChangeTracker）。</description></item>
    ///   <item><description>消费者侧（dispatcher）的扫描应只读"已到期可投递"的消息（参考 <see cref="MessageEnvelope.NextRetryAt"/>）。</description></item>
    ///   <item><description>所有方法在被 dispatcher 调用时都在 dispatcher 自己开的 UoW/事务里。</description></item>
    /// </list>
    /// </remarks>
    public interface IOutboxStorage
    {
        /// <summary>
        /// 启动时建表 / 校验 schema。容器启动时由 <c>OutboxDispatcher</c> 自动调用一次。
        /// 实现可以是幂等的 DDL（IF NOT EXISTS）或 EF 的 EnsureCreated。
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        // ============== 生产者侧：将事件追加到 outbox（必须与业务同事务） ==============

        /// <summary>
        /// 写入一条 outbox 记录。由 <c>IntegrationMessagePublisherBase</c> 在检测到活跃
        /// outbox 上下文（典型为活跃 UoW）时调用。
        /// </summary>
        /// <remarks>
        /// 实现 <b>不应</b> 直接 SaveChanges/COMMIT；写入必须搭载在外层业务事务里，
        /// 由 UoW.Commit 统一落库。否则就破坏了 outbox 模式的原子性保证。
        /// </remarks>
        Task StoreMessageAsync(MessageEnvelope message, CancellationToken cancellationToken = default);

        // ============== Dispatcher 侧：扫描待投递消息 ==============

        /// <summary>
        /// 拉取一批"已到期可投递"的 outbox 消息。
        /// </summary>
        /// <remarks>
        /// "已到期"指 <see cref="MessageEnvelope.NextRetryAt"/> 为 null 或不晚于当前 UTC 时间。
        /// 排序通常按 <see cref="MessageEnvelope.UtcTime"/> 升序以保证大致 FIFO。
        ///
        /// <para>
        /// 多 dispatcher 实例并发场景下，本接口默认实现 <b>不</b> 保证互斥（无 SELECT ... FOR UPDATE SKIP LOCKED 等数据库锁）。
        /// 重复投递的兜底由消费端 inbox 去重（<see cref="Inbox.IInboxStorage"/>）负责。
        /// </para>
        /// </remarks>
        /// <param name="maxCount">本批最大数量。</param>
        Task<IReadOnlyList<MessageEnvelope>> FetchReadyAsync(int maxCount, CancellationToken cancellationToken = default);

        // ============== Dispatcher 侧：投递成功 / 失败的回写 ==============

        /// <summary>
        /// 删除一条 outbox 记录。在 dispatcher 成功投递后调用，使下一轮扫描不再处理该行。
        /// </summary>
        /// <remarks>
        /// 实现应是幂等的：若该 id 已不存在（被并发的另一个 dispatcher 抢先删除），
        /// 不应抛异常。
        /// </remarks>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 标记一条消息投递失败，递增 <see cref="MessageEnvelope.RetryCount"/> 并设置
        /// <see cref="MessageEnvelope.NextRetryAt"/> 实现退避。
        /// </summary>
        /// <param name="id">消息 Id。</param>
        /// <param name="error">异常摘要，写入 <see cref="MessageEnvelope.LastError"/> 便于排查。</param>
        /// <param name="nextRetryAt">下次允许投递时刻（UTC），由 <c>OutboxBackoff</c> 计算。</param>
        Task MarkFailedAsync(Guid id, string error, DateTime nextRetryAt, CancellationToken cancellationToken = default);

        /// <summary>
        /// 重试次数超过 <c>OutboxOptions.MaxRetries</c> 时将消息从 outbox 表移入死信表。
        /// </summary>
        /// <remarks>
        /// 实现应在同一事务中完成"原表删除 + 死信表插入"，确保不丢消息也不重复投递。
        /// 死信表行供运维侧排查与人工补偿，不会被 dispatcher 再次扫描。
        /// </remarks>
        Task MoveToDeadLetterAsync(Guid id, string error, CancellationToken cancellationToken = default);
    }
}
