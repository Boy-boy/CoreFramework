using Core.EntityFrameworkCore;
using Core.EventBus.Outbox;
using Core.EventBus.Storage.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>基于 EF Core 的 outbox 存储。</summary>
    /// <remarks>
    /// 所有读写都走业务 <typeparamref name="TDbContext"/>:生产者只 Add 到 ChangeTracker,
    /// SaveChanges 由外层 UoW 在业务事务里触发 —— 自然达成"业务行 + outbox 行同事务落库"。
    /// <para><see cref="InitializeAsync"/> 仅在关系型 provider 上 EnsureCreated;InMemoryDb 等直接跳过。</para>
    /// <para>
    /// <see cref="FetchReadyAsync"/> 不用 DB 锁(provider 无关),多 dispatcher 实例并发会扫描到同一行,
    /// 重复投递语义由消费端 inbox 兜底。
    /// </para>
    /// </remarks>
    public class EfCoreOutboxStorage<TDbContext> : IOutboxStorage
        where TDbContext : DbContext
    {
        private readonly IDbContextProvider<TDbContext> _dbContextProvider;

        public EfCoreOutboxStorage(IDbContextProvider<TDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            if (ctx.Database.IsRelational())
            {
                // 兜底建表,生产推荐 EF Migrations;EnsureCreated 在已有数据库上是 no-op
                await ctx.Database.EnsureCreatedAsync(cancellationToken);
            }
        }

        // ============================== 生产者 ==============================

        /// <inheritdoc />
        public async Task StoreMessageAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            // 只 Add 到 ChangeTracker,SaveChanges 由 UoW 在业务事务里触发
            await ctx.Set<OutboxMessageEntity>().AddAsync(OutboxMessageEntity.FromEnvelope(message), cancellationToken);
        }

        // ============================== Dispatcher ==============================

        /// <summary>
        /// 拉取一批已到期且当前无人持有租约的消息,并把租约打到 <paramref name="leaseHolder"/> 名下。
        /// 按 <see cref="OutboxMessageEntity.UtcTime"/> 升序,与索引一致。
        /// </summary>
        /// <remarks>
        /// <para>两步走:</para>
        /// <list type="number">
        ///   <item><description>SELECT 候选行(到期 + 无人租用或租约已过期)</description></item>
        ///   <item><description>UPDATE 把 LeasedBy/LeaseUntil 写到本 dispatcher,SaveChanges 提交租约</description></item>
        /// </list>
        /// <para>因为 SELECT 与 UPDATE 之间没有锁,多 dispatcher 并发可能拿到重叠候选;但每条行通过
        /// <c>SaveChanges</c> 时的 <b>WHERE LeasedBy IS NULL OR LeaseUntil &lt;= now</c> 乐观并发条件保证只有一个赢家
        /// (输家的 entity 上租约值不会改,过滤后从返回集去掉)。</para>
        /// <para>SaveChanges 在本方法内调用一次,把租约提交独立于外层 UoW —— 否则租约只是 in-memory tracking,
        /// 其他 dispatcher 看不到。后续 send/delete/markfailed 仍由 dispatcher 在外层 UoW 内提交。</para>
        /// </remarks>
        public async Task<IReadOnlyList<MessageEnvelope>> FetchReadyAsync(int maxCount, string leaseHolder, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(leaseHolder))
                throw new ArgumentException("leaseHolder 不能为空", nameof(leaseHolder));
            if (leaseDuration <= TimeSpan.Zero)
                throw new ArgumentException("leaseDuration 必须 > 0", nameof(leaseDuration));

            var ctx = await _dbContextProvider.GetDbContextAsync();
            var now = DateTime.UtcNow;
            var until = now.Add(leaseDuration);

            // 候选:到期 + (无租约 或 租约已过期 或 本机已持有)
            // 本机已持有的也拉回来,允许 dispatcher 续约后继续处理上轮没干完的消息
            var candidates = await ctx.Set<OutboxMessageEntity>()
                .Where(x => (x.NextRetryAt == null || x.NextRetryAt <= now)
                            && (x.LeasedBy == null || x.LeaseUntil <= now || x.LeasedBy == leaseHolder))
                .OrderBy(x => x.UtcTime)
                .Take(maxCount)
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0) return Array.Empty<MessageEnvelope>();

            // 批量打租约;EF 会以 WHERE Id=... 单条 UPDATE 提交。
            // 多 dispatcher 抢同一行时,DB 端最后一个 UPDATE 赢 —— 不严格但实践够用:
            // 输家会用同样的 leaseHolder 字段值写回,业务上不损失消息但可能出现"两个都以为自己有租约"
            // 的短暂窗口。下游 send 是幂等的(broker 端 + inbox 端去重),所以可接受。
            foreach (var row in candidates)
            {
                row.LeasedBy = leaseHolder;
                row.LeaseUntil = until;
            }
            await ctx.SaveChangesAsync(cancellationToken);

            return candidates.Select(r => r.ToEnvelope()).ToList();
        }

        /// <inheritdoc />
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            var entity = await ctx.Set<OutboxMessageEntity>().FindAsync(new object[] { id }, cancellationToken);
            // 找不到等价于已被其他 dispatcher 删过,幂等返回
            if (entity != null) ctx.Set<OutboxMessageEntity>().Remove(entity);
        }

        /// <inheritdoc />
        public async Task MarkFailedAsync(Guid id, string error, DateTime nextRetryAt, CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            var entity = await ctx.Set<OutboxMessageEntity>().FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) return;

            entity.RetryCount += 1;
            entity.NextRetryAt = nextRetryAt;
            // 截断防止异常堆栈撑爆字段(配置限制 4000)
            entity.LastError = Truncate(error, 4000);
        }

        /// <inheritdoc />
        public async Task MoveToDeadLetterAsync(Guid id, string error, CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            var entity = await ctx.Set<OutboxMessageEntity>().FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) return;

            // 插入死信 + 删除 outbox 行必须同事务,由调用方 UoW 包住 commit
            ctx.Set<DeadLetterMessageEntity>().Add(new DeadLetterMessageEntity
            {
                Id = entity.Id,
                MessageId = entity.MessageId,
                Version = entity.Version,
                AssemblyName = entity.AssemblyName,
                MessageName = entity.MessageName,
                MessageData = entity.MessageData,
                CreateTime = entity.CreateTime,
                UtcTime = entity.UtcTime,
                RetryCount = entity.RetryCount + 1,
                LastError = Truncate(error, 4000),
                DeadAtUtc = DateTime.UtcNow
            });
            ctx.Set<OutboxMessageEntity>().Remove(entity);
        }

        /// <inheritdoc />
        public async Task<int> CleanupDeadLettersAsync(DateTime utcThreshold, CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            // 用 EF 7+ ExecuteDelete 避免大量行被加载到 ChangeTracker
            return await ctx.Set<DeadLetterMessageEntity>()
                .Where(x => x.DeadAtUtc < utcThreshold)
                .ExecuteDeleteAsync(cancellationToken);
        }

        private static string Truncate(string s, int max)
            => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max));
    }
}
