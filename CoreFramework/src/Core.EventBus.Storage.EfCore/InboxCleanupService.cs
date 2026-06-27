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
    /// <summary>
    /// 周期清理 inbox 表中超过保留期的记录。
    /// </summary>
    /// <remarks>
    /// <para><b>为什么需要</b></para>
    /// <para>
    /// inbox 表只增不减时会无限增长 —— 单库 100 万 / 1000 万行后查询性能下降，
    /// 备份恢复成本飙升。本服务按 <see cref="InboxOptions.RetentionDays"/> 砍掉历史。
    /// </para>
    ///
    /// <para><b>保留期的下限</b></para>
    /// <para>
    /// <see cref="InboxOptions.RetentionDays"/> 必须 &gt; broker 端可能的最大重投延迟，
    /// 否则会出现"清理后 broker 才把延迟很久的消息重投过来 → 因为 inbox 已清而被误判为新消息 → 重复处理"。
    /// </para>
    ///
    /// <para><b>启动延迟</b></para>
    /// <para>
    /// 主循环<b>先 Delay 再做事</b>：避免应用刚启动就立刻扫表给 DB 添堵。
    /// </para>
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
            while (!stoppingToken.IsCancellationRequested)
            {
                // 先睡再做：第一次清理不会和应用启动期高峰撞车
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
                    // await using var uow:storage 内部 GetDbContextAsync 会隐式 Begin UoW,
                    // 不在这里 Dispose,scope 释放也不会跑 UoW.DisposeAsync —— 残留 ambient UoW
                    // 让下一轮 BeginAsync 误返回 no-op ChildUnitOfWork。详见 OutboxDispatcher.InitializeStorageAsync。
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
                    // 清理失败不影响主流程，下次定时再尝试
                    _logger.LogError(ex, "Inbox 清理失败");
                }
            }
        }
    }
}
