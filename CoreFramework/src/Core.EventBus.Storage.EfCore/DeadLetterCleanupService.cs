using Core.EventBus.Outbox;
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
    /// <summary>周期清理死信表中超过保留期的记录。</summary>
    /// <remarks>
    /// <para>死信表只增不减:dispatcher 对超过 <see cref="OutboxOptions.MaxRetries"/> 的消息只做"原表删 + 死信插入",
    /// 不再回收;运维处理完毕(归档 / 重投 / 弃置)后通常只能手动 DELETE,容易遗忘导致表无限增长。
    /// 本服务按 <see cref="DeadLetterCleanupOptions.RetentionDays"/> 自动清理,与 inbox 清理策略对称。</para>
    /// <para>默认保留 90 天 — 远大于 inbox 的 14 天,因为死信表通常代表"需要审查"的故障样本,
    /// 直接秒清会让运维丢失排查素材。需要严格留存请通过监控订阅 <see cref="Diagnostics.EventBusMetrics.DeadLetter"/>
    /// 在归档系统(S3 / 数据仓库)再保一份。</para>
    /// <para>主循环先 Delay 再做事,避免应用刚启动就立刻扫表给 DB 添堵。</para>
    /// </remarks>
    public sealed class DeadLetterCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<DeadLetterCleanupOptions> _options;
        private readonly ILogger<DeadLetterCleanupService> _logger;

        public DeadLetterCleanupService(
            IServiceScopeFactory scopeFactory,
            IOptions<DeadLetterCleanupOptions> options,
            ILogger<DeadLetterCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 死信表与 outbox 表共用同一份 schema(由 DbContext.OnModelCreating 注册),
            // 这里不需要再单独 InitializeAsync,outbox storage 已经包含它

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
                    using var scope = _scopeFactory.CreateScope();
                    var sp = scope.ServiceProvider;
                    var uowMgr = sp.GetRequiredService<IUnitOfWorkManager>();
                    await using var uow = await uowMgr.BeginAsync(new UnitOfWorkOptions(), stoppingToken);

                    var storage = sp.GetRequiredService<IOutboxStorage>();
                    var threshold = DateTime.UtcNow.AddDays(-_options.Value.RetentionDays);
                    var deleted = await storage.CleanupDeadLettersAsync(threshold, stoppingToken);

                    await uow.CommitAsync(stoppingToken);

                    if (deleted > 0)
                    {
                        _logger.LogInformation("DeadLetter 清理删除 {Count} 条 (<{Threshold:u})", deleted, threshold);
                    }
                }
                catch (Exception ex)
                {
                    // 清理失败不影响主流程,下次定时再尝试
                    _logger.LogError(ex, "DeadLetter 清理失败");
                }
            }
        }
    }
}
