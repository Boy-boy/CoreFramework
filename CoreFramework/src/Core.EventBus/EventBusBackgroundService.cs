using Core.EventBus.Integration;
using Core.EventBus.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus
{
    /// <summary>
    /// 应用启动时把 handler 程序集中的订阅推送到 local / integration 两条 subscribe 通道。
    /// </summary>
    /// <remarks>
    /// 只承担一次性的订阅注册;outbox 投递循环由独立的 BackgroundService 承担。
    /// <para><b>非阻塞启动</b>:订阅初始化(尤其是 broker subscriber 的 ExchangeDeclare / QueueDeclare / Bind)在 broker 不可达时
    /// 可能持续数十秒。<see cref="StartAsync"/> 通过 fire-and-forget 把这块工作丢到后台 Task,避免拖慢 host 启动 /
    /// 让 dev 环境忘启 broker 时 API 直接卡死。代价:broker 上线前到达的消息会因订阅尚未就绪而暂时无人消费,
    /// 但 broker 不可达时本来也收不到消息,语义无损。</para>
    /// </remarks>
    public class EventBusBackgroundService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EventBusBackgroundService> _logger;

        // 后台订阅初始化的取消源;StopAsync 触发它让初始化任务及时退出
        private CancellationTokenSource _initCts;

        // 后台订阅初始化的任务句柄;StopAsync 等待它收尾再返回,避免 host 已停 init 还在跑
        private Task _initTask;

        public EventBusBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<EventBusBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        /// <summary>启动时把订阅初始化丢到后台,立即返回。</summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // 与 stoppingToken 解耦:StopAsync 触发自身 cts,允许在 host 关停时及时退出 init 循环
            _initCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _initTask = Task.Run(() => InitializeSubscriptionsAsync(_initCts.Token), CancellationToken.None);
            return Task.CompletedTask;
        }

        // 失败重试参数:初始 1s,每次翻倍,封顶 60s。broker 临时不可达时这个节奏不会刷屏
        private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(60);

        /// <summary>
        /// 订阅初始化主循环;失败按指数退避无上限重试,直到 stoppingToken 取消。
        /// </summary>
        /// <remarks>
        /// <para>不抛异常:任何失败(broker 不可达、程序集加载错误等)都被捕获并退避重试。
        /// 这是 host 启动期 broker 还没就绪 / broker 滚动升级场景下的关键容错。</para>
        /// <para>程序集为空 / 无 handler 这种"配置层面"问题不会重试 — 它们不可能在不重启进程的情况下被修好。</para>
        /// </remarks>
        private async Task InitializeSubscriptionsAsync(CancellationToken ct)
        {
            // 程序集检查只做一次 — 这种问题靠重试不可能修好
            using (var probeScope = _serviceScopeFactory.CreateScope())
            {
                var options = probeScope.ServiceProvider.GetRequiredService<IOptions<EventBusOptions>>().Value;
                var assemblies = options.MessageHandlerAssemblies;
                if (assemblies == null || assemblies.Count == 0)
                {
                    _logger.LogWarning(
                        "EventBus 启动时未发现任何 handler 程序集。请检查是否调用了 EventBusOptions.AddConsumers(...)。");
                    return;
                }

                var handlerCount = MessageHandlerExtensions.GetHandlerTypes(assemblies).Count();
                if (handlerCount == 0)
                {
                    _logger.LogWarning(
                        "EventBus 已注册 {AssemblyCount} 个 handler 程序集,但未扫描到任何 IMessageHandler 实现。" +
                        "请确认 handler 是否为 public 非抽象类、且实现 IMessageHandler<T>。",
                        assemblies.Count);
                    return;
                }

                _logger.LogInformation(
                    "EventBus 在 {AssemblyCount} 个程序集中扫描到 {HandlerCount} 个 handler 实现,开始挂载订阅...",
                    assemblies.Count, handlerCount);
            }

            // 订阅挂载:失败指数退避重试
            var delay = InitialRetryDelay;
            var attempt = 0;
            while (!ct.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    await TryInitSubscribersOnceAsync(ct).ConfigureAwait(false);
                    if (attempt > 1)
                    {
                        _logger.LogInformation(
                            "EventBus 订阅在第 {Attempt} 次尝试后成功挂载。", attempt);
                    }
                    return;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // host 关停期间正常取消,无需告警
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "EventBus 订阅初始化失败(第 {Attempt} 次),将在 {Delay} 后重试。",
                        attempt, delay);
                    try
                    {
                        await Task.Delay(delay, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        return;
                    }
                    // 指数退避:1s → 2s → 4s → 8s → ... → 60s 封顶
                    delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, MaxRetryDelay.TotalMilliseconds));
                }
            }
        }

        /// <summary>一次订阅挂载尝试;失败抛异常由外层重试。</summary>
        private async Task TryInitSubscribersOnceAsync(CancellationToken ct)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            var assemblies = provider.GetRequiredService<IOptions<EventBusOptions>>().Value.MessageHandlerAssemblies;

            // subscriber 是可选的:未引用对应模块时一端可能为 null
            var localSubscriber = provider.GetService<ILocalSubscriber>();
            var integrationSubscriber = provider.GetService<IIntegrationSubscriber>();
            if (localSubscriber != null)
            {
                await localSubscriber.InitializeAsync(assemblies, ct).ConfigureAwait(false);
            }
            if (integrationSubscriber != null)
            {
                await integrationSubscriber.InitializeAsync(assemblies, ct).ConfigureAwait(false);
            }
        }

        /// <summary>触发 init 取消,等待后台任务安全结束。</summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _initCts?.Cancel();
            if (_initTask != null)
            {
                try { await _initTask.ConfigureAwait(false); }
                catch { /* 后台异常已在 InitializeSubscriptionsAsync 内日志 */ }
            }
            _initCts?.Dispose();
        }
    }
}
