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

        /// <summary>拉取一批已到期可投递的消息;按 <see cref="OutboxMessageEntity.UtcTime"/> 升序,与索引一致。</summary>
        public async Task<IReadOnlyList<MessageEnvelope>> FetchReadyAsync(int maxCount, CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            var now = DateTime.UtcNow;
            // NextRetryAt == null:首次或上轮成功;<= now:失败后退避到期
            var rows = await ctx.Set<OutboxMessageEntity>()
                .Where(x => x.NextRetryAt == null || x.NextRetryAt <= now)
                .OrderBy(x => x.UtcTime)
                .Take(maxCount)
                .ToListAsync(cancellationToken);

            return rows.Select(r => r.ToEnvelope()).ToList();
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

        private static string Truncate(string s, int max)
            => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max));
    }
}
