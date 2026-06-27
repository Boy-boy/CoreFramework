using Core.EventBus.Inbox;
using Core.Uow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>周期清理 inbox 表超过保留期的记录。</summary>
    /// <remarks>
    /// inbox 只增不减会无限增长,本服务按 <see cref="InboxOptions.RetentionDays"/> 砍掉历史。
    /// <para>
    /// <see cref="InboxOptions.RetentionDays"/> 必须 &gt; broker 端可能的最大重投延迟,
    /// 否则会出现"清理后 broker 才把延迟很久的消息重投过来 → inbox 已清 → 误判为新消息 → 重复处理"。
    /// </para>
    /// <para>主循环先 Delay 再做事,避免应用刚启动就立刻扫表给 DB 添堵。</para>
    /// </remarks>
    public sealed class InboxCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<InboxOptions> _options;
        private readonly ILogger<InboxCleanupService> _logger;

        public InboxCleanupService(
            IServiceScopeFactory scopeFactory,
            IOptions<InboxOptions> options,
            ILogger<InboxCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitializeStorageAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // 先睡再做:首次清理不会和启动期高峰撞车
                try
                {
                    await Task.Delay(_options.Value.CleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    // 必须 await using uow:storage 内部 GetDbContextAsync 隐式 Begin UoW,
                    // 不在此 Dispose 会让残留 ambient UoW 干扰下一轮 BeginAsync(详见 OutboxDispatcher.InitializeStorageAsync)
                    using var scope = _scopeFactory.CreateScope();
                    var sp = scope.ServiceProvider;
                    var uowMgr = sp.GetRequiredService<IUnitOfWorkManager>();
                    await using var uow = await uowMgr.BeginAsync(new UnitOfWorkOptions(), stoppingToken);

                    var inbox = sp.GetRequiredService<IInboxStorage>();
                    var threshold = DateTime.UtcNow.AddDays(-_options.Value.RetentionDays);
                    var deleted = await inbox.CleanupAsync(threshold, stoppingToken);

                    await uow.CommitAsync(stoppingToken);

                    if (deleted > 0)
                    {
                        _logger.LogInformation("Inbox 清理删除 {Count} 条 (<{Threshold:u})", deleted, threshold);
                    }
                }
                catch (Exception ex)
                {
                    // 清理失败不影响主流程,下次定时再尝试
                    _logger.LogError(ex, "Inbox 清理失败");
                }
            }
        }

        /// <summary>启动时建表;受 <see cref="InboxOptions.AutoInitialize"/> 控制。失败仅日志不抛,与 OutboxDispatcher 一致。</summary>
        /// <remarks>
        /// 必须显式 await using uow:storage 内部 GetDbContextAsync 隐式 Begin UoW,
        /// 不在此 Dispose 时 scope 释放不会跑 UoW.DisposeAsync —— 残留 ambient UoW 会让下一轮 BeginAsync
        /// 误返回 no-op ChildUnitOfWork,后续 Commit/Rollback 被静默吞掉。
        /// </remarks>
        private async Task InitializeStorageAsync(CancellationToken ct)
        {
            if (!_options.Value.AutoInitialize) return;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;
                var uowMgr = sp.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await uowMgr.BeginAsync(new UnitOfWorkOptions(), ct);

                var storage = sp.GetRequiredService<IInboxStorage>();
                await storage.InitializeAsync(ct);

                await uow.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InboxCleanupService 初始化存储失败");
            }
        }
    }
}
