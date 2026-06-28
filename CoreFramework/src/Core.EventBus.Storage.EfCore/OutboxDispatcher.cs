using Core.EventBus.Diagnostics;
using Core.EventBus.Outbox;
using Core.Uow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>Outbox 投递后台服务,随应用启动 / 停止生命周期管理。</summary>
    /// <remarks>
    /// <para>主循环:</para>
    /// <list type="number">
    ///   <item><description>启动时一次 <see cref="IOutboxStorage.InitializeAsync"/>(受 <see cref="OutboxOptions.AutoInitialize"/> 控制)</description></item>
    ///   <item><description>每轮:<see cref="IOutboxStorage.FetchReadyAsync"/> 用应用层租约拉一批(独立 UoW 提交租约)</description></item>
    ///   <item><description>逐条<b>独立 UoW</b>处理:<see cref="IOutboxRawSender.SendRawAsync"/> → 成功 Delete / 失败 MarkFailed / 进死信</description></item>
    ///   <item><description>空批 Delay,非空立即拉下一批</description></item>
    /// </list>
    /// <para>主循环任何异常都被 catch,日志 + Delay 后继续,不会让进程退出;<see cref="OperationCanceledException"/> 作为正常退出路径不日志。</para>
    /// <para>
    /// <b>按条独立 UoW</b>:每条消息一个事务,避免整批共享 UoW 时:
    /// <list type="bullet">
    ///   <item><description>批中一条失败(如 MarkFailed 时 DB 故障)整批回滚,broker 已收的消息全部要重投</description></item>
    ///   <item><description>批中长 send(慢 broker)期间 DbContext 长时间持有连接,连接池可能耗尽</description></item>
    /// </list>
    /// 代价:每条多一次 commit IO,但单 commit 小;权衡上可控性更高。
    /// </para>
    /// <para>
    /// <b>HA 多实例</b>:<see cref="IOutboxStorage.FetchReadyAsync"/> 用应用层租约
    /// (<see cref="Entities.OutboxMessageEntity.LeasedBy"/> / <see cref="Entities.OutboxMessageEntity.LeaseUntil"/>)
    /// 实现行级互斥,持有者崩溃后租约过期可被其他实例抢占,允许多实例 HA 部署。
    /// 极端竞态下 broker 可能收双发,由消费端 inbox 兜底;<b>仍只保证至少一次,不强求恰好一次</b>。
    /// </para>
    /// </remarks>
    public class OutboxDispatcher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<OutboxOptions> _options;
        private readonly ILogger<OutboxDispatcher> _logger;

        /// <summary>本实例的租约持有者标识(进程内唯一);写入 OutboxMessageEntity.LeasedBy 供 HA 多实例互斥。</summary>
        private readonly string _leaseHolder = "dispatcher-" + Guid.NewGuid().ToString("N").Substring(0, 12);

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

        /// <summary>
        /// 拉一批后逐条独立处理。
        /// FetchReadyAsync 在独立 scope+UoW 内提交租约(让其他 dispatcher 即刻可见);
        /// 每条消息处理(send + DB 更新)在自己 scope+UoW 内独立 commit。
        /// </summary>
        /// <returns>本批实际处理条数(0 表示空轮询,调用方据此决定是否 Delay)。</returns>
        private async Task<int> ProcessBatchAsync(CancellationToken stoppingToken)
        {
            // 1) 用独立 scope+UoW 拉取并提交租约,让租约对其他 dispatcher 立刻可见。
            //    租约时长 = 单批最长预期处理时间 + 缓冲;过保后其他 dispatcher 可抢占,
            //    本实例崩溃不会让行被永久锁住。
            var leaseDuration = TimeSpan.FromTicks(_options.Value.PollInterval.Ticks * 4 + TimeSpan.FromSeconds(30).Ticks);
            IReadOnlyList<MessageEnvelope> batch;
            using (var fetchScope = _scopeFactory.CreateScope())
            {
                var sp = fetchScope.ServiceProvider;
                var uowMgr = sp.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await uowMgr.BeginAsync(new UnitOfWorkOptions(isTransactional: true), stoppingToken);
                try
                {
                    var storage = sp.GetRequiredService<IOutboxStorage>();
                    batch = await storage.FetchReadyAsync(
                        _options.Value.BatchSize, _leaseHolder, leaseDuration, stoppingToken);
                    await uow.CommitAsync(stoppingToken);
                }
                catch
                {
                    await uow.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }

            if (batch.Count == 0) return 0;

            // 2) 逐条独立 scope+UoW 处理。一条失败不影响其他条,broker IO 不长时间占连接池
            int processed = 0;
            foreach (var msg in batch)
            {
                if (stoppingToken.IsCancellationRequested) break;
                using var msgScope = _scopeFactory.CreateScope();
                var sp = msgScope.ServiceProvider;
                var uowMgr = sp.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await uowMgr.BeginAsync(new UnitOfWorkOptions(isTransactional: true), stoppingToken);
                try
                {
                    var storage = sp.GetRequiredService<IOutboxStorage>();
                    var sender = sp.GetRequiredService<IOutboxRawSender>();
                    await DispatchOneAsync(storage, sender, msg, stoppingToken);
                    await uow.CommitAsync(stoppingToken);
                    processed++;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    await uow.RollbackAsync(CancellationToken.None);
                    break;
                }
                catch (Exception ex)
                {
                    // 单条处理 DB commit 失败:回滚后继续下一条,避免一条 DB 故障阻塞整批
                    _logger.LogError(ex,
                        "Outbox 单条提交失败 OutboxId={OutboxId} MessageId={MessageId},跳过下一条",
                        msg.Id, msg.MessageId);
                    await uow.RollbackAsync(CancellationToken.None);
                }
            }
            return processed;
        }

        /// <summary>处理单条消息:投递 → 成功删 / 失败标记重试或转死信。</summary>
        private async Task DispatchOneAsync(IOutboxStorage storage, IOutboxRawSender sender,
            MessageEnvelope msg, CancellationToken ct)
        {
            var messageNameTag = new KeyValuePair<string, object>("messageName", msg.MessageName ?? "<unknown>");
            var sw = Stopwatch.StartNew();
            try
            {
                await sender.SendRawAsync(msg, ct);
                await storage.DeleteAsync(msg.Id, ct);
                sw.Stop();
                EventBusMetrics.OutboxDispatched.Add(1, messageNameTag);
                EventBusMetrics.OutboxDispatchDuration.Record(sw.Elapsed.TotalMilliseconds, messageNameTag);
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
                sw.Stop();
                // RetryCount 语义:已经失败的次数;本次失败前是 msg.RetryCount,失败后 +1
                var nextRetry = msg.RetryCount + 1;
                // 永久性错误直接转死信,避免无意义占用 outbox 并刷屏日志
                var permanent = IsPermanentFailure(ex);
                if (permanent || nextRetry > _options.Value.MaxRetries)
                {
                    var reason = permanent ? "permanent" : "maxRetries";
                    var reasonLabel = permanent ? "永久性错误" : $"超过最大重试次数 {_options.Value.MaxRetries}";
                    _logger.LogError(ex,
                        "Outbox 消息 {Reason} 转入死信 OutboxId={OutboxId} MessageId={MessageId} {MessageName}",
                        reasonLabel, msg.Id, msg.MessageId, msg.MessageName);
                    await storage.MoveToDeadLetterAsync(msg.Id, BuildErrorSummary(ex), ct);
                    EventBusMetrics.DeadLetter.Add(1,
                        messageNameTag,
                        new KeyValuePair<string, object>("reason", reason));
                }
                else
                {
                    var next = OutboxBackoff.CalculateNextRetry(nextRetry, _options.Value, DateTime.UtcNow, msg.MessageId);
                    _logger.LogWarning(ex,
                        "Outbox 消息投递失败 OutboxId={OutboxId} MessageId={MessageId} 第 {Retry} 次将在 {Next:u} 重试",
                        msg.Id, msg.MessageId, nextRetry, next);
                    await storage.MarkFailedAsync(msg.Id, BuildErrorSummary(ex), next, ct);
                    EventBusMetrics.OutboxFailed.Add(1, messageNameTag);
                }
            }
        }

        /// <summary>判定异常是否为"重试也不会成功"的永久性错误,直接转死信。</summary>
        /// <remarks>
        /// 瞬时:网络 / 连接 / 超时 → 退避重试;
        /// 永久:类型加载 / 程序集找不到 / 序列化 / payload 非法 → 转死信;
        /// 其余未知默认视为瞬时,留给 MaxRetries 兜底。
        /// <para>
        /// <b>不再把 <see cref="ArgumentException"/> 视为永久错误</b> —— broker client 内部某些
        /// 状态校验在边界场景下会抛 <see cref="ArgumentException"/>(典型如 channel 已关闭后仍 Publish),
        /// 这些是瞬时故障,误判进死信会丢业务事件。
        /// </para>
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
                case JsonException:
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
