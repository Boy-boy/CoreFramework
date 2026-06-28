using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Outbox
{
    /// <summary>Outbox(发件箱)持久化抽象;解决"业务写库成功但事件未发 / 事件已发但业务回滚"的双写问题。</summary>
    /// <remarks>
    /// 思路:把待投递消息作为一行业务数据与业务变更落在 <b>同一事务</b>,由后台 dispatcher 周期扫描后推送到 broker。
    /// 实现者关键约束:生产者写入必须与业务共享事务(通常 Add 到业务 DbContext 的 ChangeTracker);
    /// dispatcher 扫描应只读"已到期"行(参考 <see cref="MessageEnvelope.NextRetryAt"/>)。
    /// </remarks>
    public interface IOutboxStorage
    {
        /// <summary>启动时建表 / 校验 schema;实现应幂等(IF NOT EXISTS 或 EnsureCreated)。</summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>写入一条 outbox 记录;由 publisher 在检测到 outbox 上下文时调用。</summary>
        /// <remarks>
        /// 实现 <b>不应</b> SaveChanges/COMMIT,必须搭载在外层业务事务里由 UoW.Commit 统一落库,
        /// 否则破坏 outbox 的原子性保证。
        /// </remarks>
        Task StoreMessageAsync(MessageEnvelope message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 拉取一批已到期且当前无人持有租约的消息,并把它们的租约打到 <paramref name="leaseHolder"/> 名下,
        /// 防止多 dispatcher 实例并发拉到同一行。
        /// 通常按 <see cref="MessageEnvelope.UtcTime"/> 升序保证大致 FIFO。
        /// </summary>
        /// <remarks>
        /// <para>HA 部署多 dispatcher 实例时:每个 dispatcher 启动期生成自己的 <paramref name="leaseHolder"/> (通常是 <c>Guid.NewGuid()</c>),
        /// 每次拉取传入 <paramref name="leaseDuration"/>(略大于一批的最长预期处理时间)。
        /// 实现要保证"过期租约可被其他 dispatcher 抢占"以防持有者崩溃后行被永久锁住。</para>
        /// <para>本方法返回前应已提交租约写入(不依赖外层 UoW),否则两个 dispatcher 可能"并发拉同一行 + 各自回滚抢占"造成活锁。</para>
        /// <para>如果不需要 HA,业务侧可以单实例部署,租约依旧生效但无竞争。</para>
        /// </remarks>
        /// <param name="maxCount">本批最大数量。</param>
        /// <param name="leaseHolder">当前 dispatcher 实例的唯一标识(<= 128 字符);用于在 entity 上记录 LeasedBy。</param>
        /// <param name="leaseDuration">租约持续时间;过期后其他 dispatcher 可抢占。</param>
        Task<IReadOnlyList<MessageEnvelope>> FetchReadyAsync(int maxCount, string leaseHolder, TimeSpan leaseDuration, CancellationToken cancellationToken = default);

        /// <summary>删除一条 outbox 记录;dispatcher 投递成功后调用。</summary>
        /// <remarks>实现需幂等:id 已不存在(被并发 dispatcher 抢删)不应抛异常。</remarks>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>标记投递失败;递增 <see cref="MessageEnvelope.RetryCount"/> 并设置 <see cref="MessageEnvelope.NextRetryAt"/>。</summary>
        /// <param name="id">消息 Id。</param>
        /// <param name="error">异常摘要,写入 <see cref="MessageEnvelope.LastError"/>。</param>
        /// <param name="nextRetryAt">下次允许投递时刻(UTC),由 <c>OutboxBackoff</c> 计算。</param>
        Task MarkFailedAsync(Guid id, string error, DateTime nextRetryAt, CancellationToken cancellationToken = default);

        /// <summary>超过 <c>OutboxOptions.MaxRetries</c> 时将消息移入死信表。</summary>
        /// <remarks>实现需在同一事务中完成"原表删除 + 死信表插入",避免丢失或重复投递。</remarks>
        Task MoveToDeadLetterAsync(Guid id, string error, CancellationToken cancellationToken = default);

        /// <summary>删除 <c>DeadAtUtc &lt; utcThreshold</c> 的历史死信记录;返回删除行数供监控。</summary>
        /// <remarks>由后台 <c>DeadLetterCleanupService</c> 定期调用,避免死信表无界增长。
        /// 实现建议用 <c>ExecuteDeleteAsync</c> 避免大量行被加载到 ChangeTracker。</remarks>
        Task<int> CleanupDeadLettersAsync(DateTime utcThreshold, CancellationToken cancellationToken = default);
    }
}
