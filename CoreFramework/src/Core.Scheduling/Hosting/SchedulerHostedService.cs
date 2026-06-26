using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Internal;
using Core.Scheduling.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Scheduling.Hosting
{
    /// <summary>
    /// 默认调度宿主：BackgroundService + 主循环轮询，无外部依赖，适合单节点或轻量集群。
    /// <para>
    /// 每 <see cref="BackgroundSchedulingOptions.IdleDelay"/> 扫一次注册表:NextRunTime 已过且未在跑(或允许并发)即派发到独立 Task。
    /// 每次派发都过 <see cref="Abstractions.IDistributedHandlerLock"/> 仲裁——默认 noop,集群部署换 Redis 实现即可获得跨节点互斥;
    /// 持锁期间按 <see cref="BackgroundSchedulingOptions.DistributedLockRenewalFraction"/> 自动续租,防止 handler 执行超过租约导致并发执行。
    /// 停机时按 <see cref="BackgroundSchedulingOptions.ShutdownGraceTimeout"/> 等 in-flight handler 收尾后再退出。
    /// </para>
    /// </summary>
    internal sealed class SchedulerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IScheduledHandlerRegistry _registry;
        private readonly HandlerStateStore _stateStore;
        private readonly INextRunStrategy _nextRunStrategy;
        private readonly TimeProvider _timeProvider;
        private readonly BackgroundSchedulingOptions _options;
        private readonly ILogger<SchedulerHostedService> _logger;

        private readonly ConcurrentDictionary<Task, byte> _runningTasks = new();

        /// <summary>初始化默认调度宿主。<see cref="IScheduledHandlerRegistry"/> 必须按 singleton 注册。</summary>
        public SchedulerHostedService(
            IServiceScopeFactory scopeFactory,
            IScheduledHandlerRegistry registry,
            HandlerStateStore stateStore,
            INextRunStrategy nextRunStrategy,
            TimeProvider timeProvider,
            IOptions<BackgroundSchedulingOptions> options,
            ILogger<SchedulerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _registry = registry;
            _stateStore = stateStore;
            _nextRunStrategy = nextRunStrategy;
            _timeProvider = timeProvider;
            _options = options.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            InitializeHandlers();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DispatchDueHandlers(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Scheduler loop iteration failed; will retry after IdleDelay.");
                }

                try
                {
                    await Task.Delay(_options.IdleDelay, _timeProvider, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 一次性枚举注册表里所有 handler，给 store 创建条目并把 NextRunTime 初始化为 now + StartDelay。
        /// </summary>
        private void InitializeHandlers()
        {
            var now = _timeProvider.GetUtcNow();

            foreach (var handler in _registry.GetHandlers())
            {
                var record = _stateStore.Get(handler.HandlerCode);
                // 初始 NextRunTime = now + StartDelay，避免大堆 handler 同时启动撞车
                var startAt = now + handler.Schedule.StartDelay;
                record.MarkInitialized(startAt);
            }
        }

        /// <summary>
        /// 扫描所有 handler，到期者立刻派发到独立 Task。本方法本身不阻塞。
        /// </summary>
        private void DispatchDueHandlers(CancellationToken stoppingToken)
        {
            var now = _timeProvider.GetUtcNow();

            foreach (var handler in _registry.GetHandlers())
            {
                if (stoppingToken.IsCancellationRequested) break;

                var record = _stateStore.Get(handler.HandlerCode);
                record.ReadDispatchSnapshot(out var nextRunTime, out var isRunning);
                if (nextRunTime is { } next && now < next) continue;
                if (isRunning && !handler.Schedule.AllowConcurrentExecution) continue;

                // 乐观推进 NextRunTime 一个间隔，防止本节点连续轮询里重复触发
                var tentativeNext = now + (handler.Schedule.Interval > TimeSpan.Zero
                    ? handler.Schedule.Interval
                    : _options.IdleDelay);
                record.MarkStarted(now, tentativeNext);

                var captured = handler.HandlerCode;
                var task = Task.Run(() => ExecuteOnceAsync(captured, now, stoppingToken), stoppingToken);
                TrackTask(task);
            }
        }

        /// <summary>
        /// 一次 handler 执行：建作用域→抢分布式锁→构造上下文→过管线→由 <c>StateTrackingFilter</c> 收尾。
        /// 单节点默认走 noop 锁,零开销;集群部署用 Redis 实现替换 <see cref="IDistributedHandlerLock"/> 即可。
        /// </summary>
        private async Task ExecuteOnceAsync(string handlerCode, DateTimeOffset scheduledTime, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IHandlerExecutionPipeline>();
            var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedHandlerLock>();

            var handler = _registry.Find(handlerCode);
            if (handler is null)
            {
                _logger.LogWarning("Handler {HandlerCode} disappeared from registry between dispatch and run.", handlerCode);
                return;
            }

            var fireTime = _timeProvider.GetUtcNow();
            var leaseDuration = _options.DistributedLockLeaseDuration;
            if (leaseDuration <= TimeSpan.Zero)
                leaseDuration = TimeSpan.FromSeconds(30);

            IDistributedHandlerLockHandle lockHandle;
            try
            {
                lockHandle = await distributedLock
                    .TryAcquireAsync(handlerCode, leaseDuration, stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 锁实现自己挂了:本节点跳过本轮,下一 IdleDelay 再试
                _logger.LogError(ex, "Distributed lock acquire failed for {HandlerCode}; skipping this tick.", handlerCode);
                MarkSkipped(handlerCode, fireTime, "Distributed lock acquire failed.", handler.Schedule);
                return;
            }

            if (lockHandle is null)
            {
                // 别的节点持有锁;把本节点状态推进到下一周期,不算失败
                _logger.LogDebug("Handler {HandlerCode} skipped: distributed lock held by another node.", handlerCode);
                MarkSkipped(handlerCode, fireTime, "Lock held by another node.", handler.Schedule);
                return;
            }

            await using (lockHandle.ConfigureAwait(false))
            {
                // 持锁期间起心跳,防止 handler 执行时间 > 租约导致别的节点抢锁重叠执行
                using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                Task heartbeatTask = null;
                if (_options.EnableDistributedLockRenewal)
                {
                    var fraction = _options.DistributedLockRenewalFraction;
                    if (fraction <= 0 || fraction >= 1) fraction = 0.5;
                    var renewalInterval = TimeSpan.FromMilliseconds(leaseDuration.TotalMilliseconds * fraction);
                    heartbeatTask = RenewLockPeriodicallyAsync(lockHandle, renewalInterval, leaseDuration, heartbeatCts.Token);
                }

                var context = new HandlerExecutionContext(handlerCode, scheduledTime, fireTime, scope.ServiceProvider);

                try
                {
                    // 异常会被 StateTrackingFilter 兜底成 Faulted 结果;不会冒到这里。
                    await pipeline.InvokeAsync(handler, context, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // 万一 StateTrackingFilter 自己挂了
                    _logger.LogError(ex, "Pipeline outermost catch: handler {HandlerCode} faulted.", handlerCode);
                }
                finally
                {
                    // 先停心跳再 dispose,避免心跳给已释放的锁续租
                    await heartbeatCts.CancelAsync();
                    if (heartbeatTask is not null)
                    {
                        try { await heartbeatTask.ConfigureAwait(false); }
                        catch (OperationCanceledException) { /* 心跳被我们 cancel 的,忽略 */ }
                    }
                }
            }
        }

        /// <summary>
        /// 心跳续租循环。每 <paramref name="renewalInterval"/> 调一次 <see cref="IDistributedHandlerLockHandle.RenewAsync"/>。
        /// 续不上(返回 false)即结束循环,后续由 dispose 兜底释放;锁失效的窗口里 handler 可能重叠执行,只能告警。
        /// </summary>
        private async Task RenewLockPeriodicallyAsync(
            IDistributedHandlerLockHandle handle,
            TimeSpan renewalInterval,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(renewalInterval, _timeProvider, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                try
                {
                    var renewed = await handle.RenewAsync(leaseDuration, cancellationToken).ConfigureAwait(false);
                    if (!renewed)
                    {
                        _logger.LogWarning(
                            "Distributed lock for handler {HandlerCode} could not be renewed; another node may have taken it. "
                            + "Handler may now run concurrently across nodes until completion.",
                            handle.HandlerCode);
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Distributed lock renewal threw for handler {HandlerCode}; will retry next interval.",
                        handle.HandlerCode);
                }
            }
        }

        /// <summary>抢锁失败/异常时,手动收尾状态:Skipped + 下一周期。
        /// 不走 pipeline,所以这里手动同时调用 MarkFinished(写共享状态) + TryUpdateNextRunTime(BG 算下次)。</summary>
        private void MarkSkipped(string handlerCode, DateTimeOffset fireTime, string reason, ScheduleDescriptor schedule)
        {
            var finish = _timeProvider.GetUtcNow();
            var record = _stateStore.Get(handlerCode);
            record.MarkFinished(
                finish,
                HandlerExecutionResult.Skipped(handlerCode, fireTime, finish, reason));
            record.TryUpdateNextRunTime(schedule, _nextRunStrategy);
        }

        private void TrackTask(Task task)
        {
            _runningTasks.TryAdd(task, 0);
            task.ContinueWith(
                static (t, state) => ((ConcurrentDictionary<Task, byte>)state!).TryRemove(t, out _),
                _runningTasks,
                TaskScheduler.Default);
        }

        /// <inheritdoc />
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // 触发 BackgroundService 自己的停止信号
            await base.StopAsync(cancellationToken).ConfigureAwait(false);

            var pending = _runningTasks.Keys.Where(static t => !t.IsCompleted).ToArray();
            if (pending.Length == 0) return;

            _logger.LogInformation(
                "Waiting up to {Timeout} for {Count} in-flight handler(s) to complete.",
                _options.ShutdownGraceTimeout,
                pending.Length);

            // WhenAny 本身不抛;Delay 的 OCE 不会冒出来。cancellationToken 作用是外部强制停止时立即返回。
            await Task.WhenAny(
                Task.WhenAll(pending),
                Task.Delay(_options.ShutdownGraceTimeout, _timeProvider, cancellationToken))
                .ConfigureAwait(false);
        }
    }
}
