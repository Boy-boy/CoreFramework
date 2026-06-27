using Core.EventBus.Outbox;
using Core.Uow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>Outbox 投递后台服务,随应用启动 / 停止生命周期管理。</summary>
    /// <remarks>
    /// <para>主循环:</para>
    /// <list type="number">
    ///   <item><description>启动时一次 <see cref="IOutboxStorage.InitializeAsync"/>(受 <see cref="OutboxOptions.AutoInitialize"/> 控制)</description></item>
    ///   <item><description>每轮:transactional UoW + <see cref="IOutboxStorage.FetchReadyAsync"/> 拉一批</description></item>
    ///   <item><description>逐条 <see cref="IOutboxRawSender.SendRawAsync"/> 投递到 broker</description></item>
    ///   <item><description>成功 Delete;失败 MarkFailed 或 MoveToDeadLetter</description></item>
    ///   <item><description>批结束 commit;空批 Delay,非空立即拉下一批</description></item>
    /// </list>
    /// <para>主循环任何异常都被 catch,日志 + Delay 后继续,不会让进程退出;<see cref="OperationCanceledException"/> 作为正常退出路径不日志。</para>
    /// <para>
    /// 整批共享一个 dispatcher UoW:批内"投递 + 删除"原子提交。若 dispatcher 崩溃,broker 可能已收到但行未删,
    /// 重复投递由消费端 inbox 兜底;<b>只保证至少一次,不强求恰好一次</b>。
    /// </para>
    /// <para>
    /// <see cref="IOutboxStorage.FetchReadyAsync"/> 无 DB 锁,多 dispatcher 并发会扫描到同一行(inbox 兜底)。
    /// 生产建议单实例;如需多实例严格不重复,可在 storage 中加 <c>FOR UPDATE SKIP LOCKED</c> 或租约字段。
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
                        // 空轮询按配置间隔等待;非空立即拉下一批以摊薄延迟
                        await SafeDelayAsync(_options.Value.PollInterval, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // 吞掉未预期异常,保证 dispatcher 不因偶发故障退出
                    _logger.LogError(ex, "OutboxDispatcher 主循环异常，将在 {Delay} 后重试", _options.Value.PollInterval);
                    await SafeDelayAsync(_options.Value.PollInterval, stoppingToken);
                }
            }
        }

        /// <summary>启动时尝试初始化存储(典型为 EnsureCreated);失败仅日志不抛,避免初始化故障让进程一直 crash 重启。</summary>
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

                var storage = sp.GetRequiredService<IOutboxStorage>();
                await storage.InitializeAsync(ct);

                await uow.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxDispatcher 初始化存储失败");
            }
        }

        /// <summary>处理一批 outbox 消息,整批共享一个 transactional UoW。</summary>
        /// <returns>本批实际处理条数(0 表示空轮询,调用方据此决定是否 Delay)。</returns>
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
                // 用 CancellationToken.None 确保即使 stoppingToken 已 cancel,回滚也能完成
                await uow.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        /// <summary>处理单条消息:投递 → 成功删 / 失败标记重试或转死信。</summary>
        private async Task DispatchOneAsync(IOutboxStorage storage, IOutboxRawSender sender,
            MessageEnvelope msg, CancellationToken ct)
        {
            try
            {
                await sender.SendRawAsync(msg, ct);
                await storage.DeleteAsync(msg.Id, ct);
                _logger.LogDebug("Outbox 消息已投递并删除 OutboxId={OutboxId} MessageId={MessageId} {MessageName}",
                    msg.Id, msg.MessageId, msg.MessageName);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // ct 触发的取消不算"投递失败",不递增 RetryCount;
                // 让外层 ProcessBatchAsync rollback,整批未提交,下轮重新拉
                throw;
            }
            catch (Exception ex)
            {
                // RetryCount 语义:已经失败的次数;本次失败前是 msg.RetryCount,失败后 +1
                var nextRetry = msg.RetryCount + 1;
                // 永久性错误直接转死信,避免无意义占用 outbox 并刷屏日志
                var permanent = IsPermanentFailure(ex);
                if (permanent || nextRetry > _options.Value.MaxRetries)
                {
                    var reason = permanent ? "永久性错误" : $"超过最大重试次数 {_options.Value.MaxRetries}";
                    _logger.LogError(ex,
                        "Outbox 消息 {Reason} 转入死信 OutboxId={OutboxId} MessageId={MessageId} {MessageName}",
                        reason, msg.Id, msg.MessageId, msg.MessageName);
                    await storage.MoveToDeadLetterAsync(msg.Id, BuildErrorSummary(ex), ct);
                }
                else
                {
                    var next = OutboxBackoff.CalculateNextRetry(nextRetry, _options.Value, DateTime.UtcNow, msg.MessageId);
                    _logger.LogWarning(ex,
                        "Outbox 消息投递失败 OutboxId={OutboxId} MessageId={MessageId} 第 {Retry} 次将在 {Next:u} 重试",
                        msg.Id, msg.MessageId, nextRetry, next);
                    await storage.MarkFailedAsync(msg.Id, BuildErrorSummary(ex), next, ct);
                }
            }
        }

        /// <summary>判定异常是否为"重试也不会成功"的永久性错误,直接转死信。</summary>
        /// <remarks>
        /// 瞬时:网络 / 连接 / 超时 → 退避重试;
        /// 永久:类型加载 / 程序集找不到 / 序列化 / payload 非法 / 参数错误 → 转死信;
        /// 其余未知默认视为瞬时,留给 MaxRetries 兜底。
        /// </remarks>
        private static bool IsPermanentFailure(Exception ex)
        {
            switch (ex)
            {
                case OperationCanceledException:    // ct 触发,不是 outbox 自身问题
                case TimeoutException:
                case SocketException:
                    return false;
                case TypeLoadException:
                case BadImageFormatException:
                case FileNotFoundException:         // 程序集/类型已删除
                case FileLoadException:
                case MissingMethodException:
                case MissingMemberException:
                case FormatException:
                case Newtonsoft.Json.JsonException:
                case ArgumentException:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>截断异常信息,防止 LastError 字段被超长堆栈撑爆。</summary>
        private static string BuildErrorSummary(Exception ex)
        {
            var text = ex.ToString();
            return text.Length > 4000 ? text.Substring(0, 4000) : text;
        }

        /// <summary>包装 <see cref="Task.Delay(TimeSpan, CancellationToken)"/>:触发取消即正常结束,避免 stop 路径产生异常噪音。</summary>
        private static async Task SafeDelayAsync(TimeSpan delay, CancellationToken ct)
        {
            try { await Task.Delay(delay, ct); }
            catch (OperationCanceledException) { }
        }
    }
}
