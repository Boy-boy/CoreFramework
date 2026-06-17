using Core.EntityFrameworkCore;
using Core.EventBus.Inbox;
using Core.EventBus.Storage.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>
    /// 基于 EF Core 的 inbox 存储实现，承担消费端"消息 × handler"幂等去重。
    /// </summary>
    /// <remarks>
    /// <para><b>使用前提</b></para>
    /// <para>
    /// 业务 <typeparamref name="TDbContext"/> 必须已经通过 <c>modelBuilder.AddEventBusStorage()</c>
    /// 加入 <see cref="InboxMessageEntity"/> 的映射。
    /// </para>
    /// <para><b>事务约定</b></para>
    /// <para>
    /// <see cref="TryAcquireAsync"/> 只 Add 到 ChangeTracker，不 SaveChanges。
    /// 真正落库由调用方所在的 UoW.Commit 触发；这是关键，否则去重就破了：
    /// 如果在 acquire 时就立刻 commit inbox 行，handler 业务失败回滚时 inbox 行不会跟着回滚，
    /// 下次重投就会被误判为"已处理"而跳过。
    /// </para>
    /// </remarks>
    public class EfCoreInboxStorage<TDbContext> : IInboxStorage
        where TDbContext : DbContext
    {
        private readonly IDbContextProvider<TDbContext> _dbContextProvider;

        public EfCoreInboxStorage(IDbContextProvider<TDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            if (ctx.Database.IsRelational())
            {
                await ctx.Database.EnsureCreatedAsync(cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<bool> TryAcquireAsync(Guid messageId, string consumerGroup, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(consumerGroup))
                throw new ArgumentNullException(nameof(consumerGroup));

            var ctx = await _dbContextProvider.GetDbContextAsync();

            // 先查再 Add 的双步检查：
            //   命中：直接返回 false，handler 跳过；
            //   未命中：Add 行（不 commit），等同事务内若并发的另一调用也 Add，commit 时会因主键冲突回滚
            // 注：这里没用 INSERT ... ON CONFLICT，因为不同 provider 语法差异；
            //   两步查询在主键冲突时仍由 DB 兜底（commit 阶段抛 DbUpdateException）
            var exists = await ctx.Set<InboxMessageEntity>()
                .AnyAsync(x => x.MessageId == messageId && x.ConsumerGroup == consumerGroup, cancellationToken);
            if (exists) return false;

            await ctx.Set<InboxMessageEntity>().AddAsync(new InboxMessageEntity
            {
                MessageId = messageId,
                ConsumerGroup = consumerGroup,
                ProcessedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }

        /// <inheritdoc />
        public async Task<int> CleanupAsync(DateTime utcThreshold, CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            // 使用 EF 7+ 的 ExecuteDelete，避免大量行被加载到 ChangeTracker
            return await ctx.Set<InboxMessageEntity>()
                .Where(x => x.ProcessedAtUtc < utcThreshold)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
