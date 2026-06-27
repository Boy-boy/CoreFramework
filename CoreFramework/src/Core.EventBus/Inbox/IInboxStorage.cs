using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Inbox
{
    /// <summary>消费端幂等存储(inbox / 接收簿);把 outbox 的"至少一次"提升到"业务上恰好一次"。</summary>
    /// <remarks>
    /// <para>主键 <c>(MessageId, ConsumerGroup)</c>:框架默认用 handler 类型全名作 ConsumerGroup,
    /// 让同一条消息被多个 handler 订阅时各自独立去重。</para>
    /// <para>典型用法(invoker 在每次调用 handler 时):</para>
    /// <code>
    /// await using var uow = mgr.Begin(transactional: true);
    /// if (!await inbox.TryAcquireAsync(msg.Id, handlerType.FullName, ct))
    /// {
    ///     await uow.CommitAsync();   // 已处理 → 跳过 handler 但仍 commit 让外层 ack
    ///     return;
    /// }
    /// await handler.HandleAsync(msg);
    /// await uow.CommitAsync();       // inbox 行 + handler 写入业务表 同事务落库
    /// </code>
    /// </remarks>
    public interface IInboxStorage
    {
        /// <summary>启动时建表 / 校验 schema;由后台清理服务调用一次。</summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>尝试登记一条接收记录。</summary>
        /// <param name="messageId">消息全局 Id(一般来自 <see cref="Outbox.MessageEnvelope.Id"/>)。</param>
        /// <param name="consumerGroup">消费方标识,框架默认传 handler 类型全名。</param>
        /// <returns><c>true</c>=首次接收,执行 handler 并 commit;<c>false</c>=已处理过,直接 commit ack broker。</returns>
        /// <remarks>
        /// 方法只 Add 到 ChangeTracker,commit 由调用方 UoW 统一负责;这样 handler 失败时 inbox 行随业务回滚,
        /// 下次重投不会误判为"已处理"。主键冲突在 commit 阶段才暴露为 <c>DbUpdateException</c>,
        /// 由 invoker 在 commit 异常路径中识别为"并发消费抢先登记"。
        /// </remarks>
        Task<bool> TryAcquireAsync(Guid messageId, string consumerGroup, CancellationToken cancellationToken = default);

        /// <summary>删除 <c>ProcessedAtUtc &lt; utcThreshold</c> 的历史记录;返回删除行数供监控。</summary>
        /// <param name="utcThreshold">阈值时刻(UTC),通常为"现在 - RetentionDays"。</param>
        Task<int> CleanupAsync(DateTime utcThreshold, CancellationToken cancellationToken = default);
    }
}
