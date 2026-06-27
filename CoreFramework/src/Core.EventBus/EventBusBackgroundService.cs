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
    /// 在应用启动时把 [MessageHandler] 标注的 handler 注册到对应的 subscriber（local / integration）。
    /// </summary>
    /// <remarks>
    /// outbox 投递循环已不在本服务范围内 —— 由 <c>Core.EventBus.Storage.EfCore.OutboxDispatcher</c>
    /// 作为独立的 <see cref="BackgroundService"/> 承担。这里只承担一次性的订阅注册任务。
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

        /// <summary>
        /// 应用启动时被 HostedService 框架调用一次：扫描 handler 程序集，
        /// 把 (messageType, handlerType) 对推送到 local / integration 两条 subscribe 通道。
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            var options = provider.GetRequiredService<IOptions<EventBusOptions>>().Value;

            var assemblies = options.MessageHandlerAssemblies;
            if (assemblies == null || assemblies.Length == 0)
            {
                // 静默 no-op 在生产排查时极难发现：用户经常忘了调 AddConsumers(...)
                // 这里显式告警，给排查留线索
                _logger.LogWarning(
                    "EventBus 启动时未发现任何 handler 程序集。请检查是否调用了 EventBusOptions.AddConsumers(...)。");
                return;
            }

            // subscriber 是可选的：项目没引用 broker 模块时 integration 一端可能为 null
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

            // 报告实际订阅数 —— 程序集存在但里面没有任何 IMessageHandler 实现时,
            // 单测 / 部署版本错位排查不再"完全没日志"
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

        /// <summary>
        /// 应用关闭时被 HostedService 框架调用。本服务无后台循环，无须清理资源 → no-op。
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
