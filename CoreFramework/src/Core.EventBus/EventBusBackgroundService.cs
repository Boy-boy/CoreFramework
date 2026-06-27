using Core.EventBus.Integration;
using Core.EventBus.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    /// </remarks>
    public class EventBusBackgroundService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EventBusBackgroundService> _logger;

        public EventBusBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<EventBusBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        /// <summary>启动时扫描 handler 程序集,把订阅对推送到 local / integration 通道。</summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            var options = provider.GetRequiredService<IOptions<EventBusOptions>>().Value;

            var assemblies = options.MessageHandlerAssemblies;
            if (assemblies == null || assemblies.Length == 0)
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
                await localSubscriber.InitializeAsync(assemblies, cancellationToken).ConfigureAwait(false);
            }
            if (integrationSubscriber != null)
            {
                await integrationSubscriber.InitializeAsync(assemblies, cancellationToken).ConfigureAwait(false);
            }

            // 报告实际订阅数:程序集存在但无 handler 实现时,部署版本错位排查不再"完全没日志"
            var handlerCount = MessageHandlerExtensions.GetHandlerTypes(assemblies).Count();
            if (handlerCount == 0)
            {
                _logger.LogWarning(
                    "EventBus 已注册 {AssemblyCount} 个 handler 程序集,但未扫描到任何 IMessageHandler 实现。" +
                    "请确认 handler 是否为 public 非抽象类、且实现 IMessageHandler<T>。",
                    assemblies.Length);
            }
            else
            {
                _logger.LogInformation(
                    "EventBus 在 {AssemblyCount} 个程序集中扫描到 {HandlerCount} 个 handler 实现",
                    assemblies.Length, handlerCount);
            }
        }

        /// <summary>无后台循环,no-op。</summary>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
