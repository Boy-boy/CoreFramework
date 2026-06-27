using Core.EntityFrameworkCore;
using Core.EventBus.Inbox;
using Core.EventBus.Storage.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>基于 EF Core 的 inbox 存储,承担"消息 × handler"幂等去重。</summary>
    /// <remarks>
    /// 业务 <typeparamref name="TDbContext"/> 必须先 <c>modelBuilder.AddEventBusStorage()</c> 加入映射。
    /// <see cref="TryAcquireAsync"/> 只 Add 到 ChangeTracker 不 SaveChanges,落库由调用方 UoW.Commit 触发 ——
    /// 否则 handler 业务失败回滚时 inbox 行不会跟着回滚,下次重投会被误判为"已处理"而跳过。
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

            // 先查再 Add:命中即跳过;未命中 Add 行(不 commit),并发由 DB 主键冲突在 commit 阶段兜底。
            // 没用 INSERT ... ON CONFLICT 是因为不同 provider 语法差异
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
            // 用 EF 7+ ExecuteDelete 避免大量行被加载到 ChangeTracker
            return await ctx.Set<InboxMessageEntity>()
                .Where(x => x.ProcessedAtUtc < utcThreshold)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
