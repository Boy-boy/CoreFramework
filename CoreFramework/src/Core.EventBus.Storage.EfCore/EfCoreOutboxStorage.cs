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
    /// <summary>
    /// 基于 EF Core 的 outbox 存储实现。
    /// </summary>
    /// <remarks>
    /// <para><b>核心思路</b></para>
    /// <para>
    /// 所有读写都通过用户的业务 <typeparamref name="TDbContext"/> 完成。生产者写入只是把
    /// 实体 Add 到 ChangeTracker —— SaveChanges 由外层 UoW 在业务事务里统一触发，
    /// 自然达成"业务行 + outbox 行同事务落库"。
    /// </para>
    /// <para><b>Provider 兼容性</b></para>
    /// <para>
    /// <see cref="InitializeAsync"/> 会判断 <see cref="DatabaseFacade.IsRelational"/>，
    /// 关系型数据库自动 <c>EnsureCreated</c>；InMemoryDb 等无 schema 概念的 provider 直接跳过。
    /// </para>
    /// <para><b>多 dispatcher 实例</b></para>
    /// <para>
    /// 本实现 <see cref="FetchReadyAsync"/> 不使用数据库锁（DBMS 无关），因此多实例并发
    /// 可能扫描到同一行。重复投递的语义由消费端 inbox（<see cref="EfCoreInboxStorage{TDbContext}"/>）兜底。
    /// 单实例场景下完全无需担心。
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
                // 仅作为兜底建表，生产推荐用 EF Migrations 管理 schema。
                // EnsureCreated 在已有数据库上是 no-op，对历史项目安全
                await ctx.Database.EnsureCreatedAsync(cancellationToken);
            }
        }

        // ============================== 生产者 ==============================

        /// <inheritdoc />
        public async Task StoreMessageAsync(MessageEnvelope message, CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            // 关键：只 Add 到 ChangeTracker，绝不调 SaveChanges。SaveChanges 由 UoW 在业务事务里触发
            await ctx.Set<OutboxMessageEntity>().AddAsync(OutboxMessageEntity.FromEnvelope(message), cancellationToken);
        }

        // ============================== Dispatcher ==============================

        /// <summary>
        /// 拉取一批"已到期可投递"的消息。
        /// 排序按 <see cref="OutboxMessageEntity.UtcTime"/> 升序，与 outbox 索引一致。
        /// </summary>
        public async Task<IReadOnlyList<MessageEnvelope>> FetchReadyAsync(int maxCount, CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            var now = DateTime.UtcNow;
            // NextRetryAt == null：首次或上轮成功不需重试 → 立即可投递
            // NextRetryAt <= now：之前失败但退避时间已到
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
            // 找不到等价于"已被其他 dispatcher 删过"，幂等返回
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
            // 截断防止异常堆栈撑爆字段；OutboxMessageConfiguration 限制 4000 字符
            entity.LastError = Truncate(error, 4000);
        }

        /// <inheritdoc />
        public async Task MoveToDeadLetterAsync(Guid id, string error, CancellationToken cancellationToken = default)
        {
            var ctx = await _dbContextProvider.GetDbContextAsync();
            var entity = await ctx.Set<OutboxMessageEntity>().FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) return;

            // "插入死信表 + 删除 outbox 行"必须同事务：dispatcher 调用者会包在自己的 UoW 里 commit
            ctx.Set<DeadLetterMessageEntity>().Add(new DeadLetterMessageEntity
            {
                Id = entity.Id,
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
