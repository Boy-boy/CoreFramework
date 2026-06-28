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

        private async Task InitializeSubscriptionsAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider;
                var options = provider.GetRequiredService<IOptions<EventBusOptions>>().Value;

                var assemblies = options.MessageHandlerAssemblies;
                if (assemblies == null || assemblies.Count == 0)
                {
                    // 静默 no-op 在生产排查极难发现:显式告警留线索
                    _logger.LogWarning(
                        "EventBus 启动时未发现任何 handler 程序集。请检查是否调用了 EventBusOptions.AddConsumers(...)。");
                    return;
                }

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

                // 报告实际订阅数:程序集存在但无 handler 实现时,部署版本错位排查不再"完全没日志"
                var handlerCount = MessageHandlerExtensions.GetHandlerTypes(assemblies).Count();
                if (handlerCount == 0)
                {
                    _logger.LogWarning(
                        "EventBus 已注册 {AssemblyCount} 个 handler 程序集,但未扫描到任何 IMessageHandler 实现。" +
                        "请确认 handler 是否为 public 非抽象类、且实现 IMessageHandler<T>。",
                        assemblies.Count);
                }
                else
                {
                    _logger.LogInformation(
                        "EventBus 在 {AssemblyCount} 个程序集中扫描到 {HandlerCount} 个 handler 实现",
                        assemblies.Count, handlerCount);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // host 关停期间正常取消,无需告警
            }
            catch (Exception ex)
            {
                // broker 不可达 / 程序集加载失败等:不让进程崩溃,等待人工介入或自动重试外部 broker
                _logger.LogError(ex,
                    "EventBus 订阅初始化失败,handler 暂未挂载;待 broker 恢复后请重启进程或扩展为重试初始化。");
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
