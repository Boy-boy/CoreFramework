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
    /// <summary>
    /// Outbox 投递后台服务。<see cref="BackgroundService"/> 实例随应用启动 / 停止生命周期管理。
    /// </summary>
    /// <remarks>
    /// <para><b>主循环</b></para>
    /// <list type="number">
    ///   <item><description>启动时调一次 <see cref="IOutboxStorage.InitializeAsync"/>（受 <see cref="OutboxOptions.AutoInitialize"/> 控制）</description></item>
    ///   <item><description>每轮：开 transactional UoW → <see cref="IOutboxStorage.FetchReadyAsync"/> 拉一批</description></item>
    ///   <item><description>逐条尝试 <see cref="IOutboxRawSender.SendRawAsync"/> 投递到 broker</description></item>
    ///   <item><description>成功 → <see cref="IOutboxStorage.DeleteAsync"/>；失败 → <see cref="IOutboxStorage.MarkFailedAsync"/> 或 <see cref="IOutboxStorage.MoveToDeadLetterAsync"/></description></item>
    ///   <item><description>批结束 → commit；批内任意一条删/写操作回滚整批（依赖 UoW 事务）</description></item>
    ///   <item><description>空批次 → <c>Task.Delay(PollInterval)</c>；非空批次立即拉下一批</description></item>
    /// </list>
    ///
    /// <para><b>容错</b></para>
    /// <list type="bullet">
    ///   <item><description>主循环任何异常都被 catch → 日志 + 等待 PollInterval → 继续，
    ///   <b>不会让进程退出</b>。</description></item>
    ///   <item><description>InitializeAsync 失败也被 catch；下一轮 Fetch 自然会再次失败暴露问题。</description></item>
    ///   <item><description><see cref="OperationCanceledException"/>（stoppingToken 触发）作为正常退出路径，不日志。</description></item>
    /// </list>
    ///
    /// <para><b>事务边界</b></para>
    /// <para>
    /// 整批用一个 dispatcher UoW 包起来：批内"投递 + 删除"作为一个原子单元提交。
    /// 这样如果 dispatcher 自身崩溃，下次启动会重新扫描到这批消息（broker 可能已收到，
    /// 但 outbox 行没删 → 重复投递）。<b>重复投递的兜底由消费端 inbox 去重负责</b>，
    /// dispatcher 端不强求"恰好一次"，只追求"至少一次"。
    /// </para>
    ///
    /// <para><b>多实例</b></para>
    /// <para>
    /// 当前实现 <see cref="IOutboxStorage.FetchReadyAsync"/> 无 DB 锁，多 dispatcher 并发会扫描到同一行 →
    /// 重复投递；inbox 兜底。生产建议单实例运行；如需多实例 + 严格不重复，可在 storage 实现
    /// 中加 <c>FOR UPDATE SKIP LOCKED</c>（PG/MySQL 8）或租约字段。
    /// </para>
    /// </remarks>
    public class OutboxDispatcher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<OutboxOptions> _options;
        private readonly ILogger<OutboxDispatcher> _logger;

        public OutboxDispatcher(
            IServiceScopeFactory scopeFactory,
            IOptions<OutboxOptions> options,
            ILogger<OutboxDispatcher> logger)
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
                try
                {
                    var processed = await ProcessBatchAsync(stoppingToken);
                    if (processed == 0)
                    {
                        // 空轮询：按配置间隔等待；非空批不等，立即拉下一批以摊薄延迟
                        await SafeDelayAsync(_options.Value.PollInterval, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // 正常关闭
                    break;
                }
                catch (Exception ex)
                {
                    // 主循环吞掉任何未预期异常，保证 dispatcher 不会因偶发故障退出。
                    // 真正的失败原因记录到日志，由运维侧告警
                    _logger.LogError(ex, "OutboxDispatcher 主循环异常，将在 {Delay} 后重试", _options.Value.PollInterval);
                    await SafeDelayAsync(_options.Value.PollInterval, stoppingToken);
                }
            }
        }

        /// <summary>
        /// 启动时尝试初始化存储（典型为 EnsureCreated）。失败也仅日志不抛 —— 让后续主循环
        /// 在真正使用时再次失败暴露问题，避免初始化故障让进程一直 crash 重启。
        /// </summary>
        private async Task InitializeStorageAsync(CancellationToken ct)
        {
            if (!_options.Value.AutoInitialize) return;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<IOutboxStorage>();
                await storage.InitializeAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxDispatcher 初始化存储失败");
            }
        }

        /// <summary>
        /// 处理一批 outbox 消息。整批共享一个 transactional UoW。
        /// </summary>
        /// <returns>本批实际处理的条数（0 表示空轮询，调用方据此决定是否 Delay）。</returns>
        private async Task<int> ProcessBatchAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var storage = sp.GetRequiredService<IOutboxStorage>();
            var sender = sp.GetRequiredService<IOutboxRawSender>();
            var uowMgr = sp.GetRequiredService<IUnitOfWorkManager>();

            await using var uow = await uowMgr.BeginAsync(
                new UnitOfWorkOptions(isTransactional: true),
                stoppingToken);

            int processed = 0;
            try
            {
                var batch = await storage.FetchReadyAsync(_options.Value.BatchSize, stoppingToken);

                foreach (var msg in batch)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    await DispatchOneAsync(storage, sender, msg, stoppingToken);
                    processed++;
                }

                await uow.CommitAsync(stoppingToken);
                return processed;
            }
            catch
            {
                // 用 CancellationToken.None 确保即使 stoppingToken 已 cancel，回滚也能完成
                await uow.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        /// <summary>
        /// 处理单条消息：投递 → 成功删 / 失败标记重试或转死信。
        /// </summary>
        private async Task DispatchOneAsync(IOutboxStorage storage, IOutboxRawSender sender,
            MessageEnvelope msg, CancellationToken ct)
        {
            try
            {
                await sender.SendRawAsync(msg, ct);
                await storage.DeleteAsync(msg.Id, ct);
                _logger.LogDebug("Outbox 消息已投递并删除 {MessageId} {MessageName}", msg.Id, msg.MessageName);
            }
            catch (Exception ex)
            {
                // 注意 RetryCount 的语义："已经失败的次数"。本次失败前是 msg.RetryCount，本次失败后是 +1
                var nextRetry = msg.RetryCount + 1;
                if (nextRetry > _options.Value.MaxRetries)
                {
                    _logger.LogError(ex,
                        "Outbox 消息超过最大重试次数 {Max} 转入死信 {MessageId} {MessageName}",
                        _options.Value.MaxRetries, msg.Id, msg.MessageName);
                    await storage.MoveToDeadLetterAsync(msg.Id, BuildErrorSummary(ex), ct);
                }
                else
                {
                    var next = OutboxBackoff.CalculateNextRetry(nextRetry, _options.Value, DateTime.UtcNow);
                    _logger.LogWarning(ex,
                        "Outbox 消息投递失败 {MessageId} 第 {Retry} 次将在 {Next:u} 重试",
                        msg.Id, nextRetry, next);
                    await storage.MarkFailedAsync(msg.Id, BuildErrorSummary(ex), next, ct);
                }
            }
        }

        /// <summary>截断异常信息防止单条 LastError 字段被超长堆栈撑爆。</summary>
        private static string BuildErrorSummary(Exception ex)
        {
            var text = ex.ToString();
            return text.Length > 4000 ? text.Substring(0, 4000) : text;
        }

        /// <summary>
        /// 包装 <see cref="Task.Delay(TimeSpan, CancellationToken)"/>：触发取消时返回正常结束，
        /// 不让 stop 路径产生 OperationCanceledException 噪音。
        /// </summary>
        private static async Task SafeDelayAsync(TimeSpan delay, CancellationToken ct)
        {
            try { await Task.Delay(delay, ct); }
            catch (OperationCanceledException) { }
        }
    }
}
