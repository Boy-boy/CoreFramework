using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Core.EventBus
{
    /// <summary>
    /// EventBus 根模块。负责"基础设施"的注册，并在所有下游模块 ConfigureServices 完成后
    /// 统一执行 <see cref="EventBusOptions"/> 的 extension 聚合 → 服务注册。
    /// </summary>
    /// <remarks>
    /// <para><b>两段式注册的原因</b></para>
    /// <para>
    /// 下游模块（Local / RabbitMQ / EfCoreStorage 等）会通过
    /// <c>services.Configure&lt;EventBusOptions&gt;(...)</c> 不断往 <see cref="EventBusOptions.Extensions"/>
    /// 里堆 extension。如果在 <see cref="ConfigureServices"/> 阶段就立即解析 options，
    /// 那时其他模块还没机会注册自己的 extension，会导致功能缺失。
    /// </para>
    /// <para>
    /// 解决方法：在 <see cref="ConfigureServices"/> 只注册"必须存在的最小集"
    /// （<see cref="EventBusBackgroundService"/> + <see cref="IMessageHandlerInvoker"/>），
    /// 真正的 publisher / subscribe 注册推迟到 <see cref="PostConfigureServices"/> 阶段统一完成。
    /// </para>
    /// </remarks>
    public class CoreEventBusModule : CoreModuleBase
    {
        /// <summary>
        /// 注册基础设施：HostedService 和默认 invoker。
        /// publisher / subscribe 等需要 <see cref="EventBusOptions"/> 聚合后才能正确注册的服务
        /// 延后到 <see cref="PostConfigureServices"/> 处理。
        /// </summary>
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            // 这里只注册"基础设施"（HostedService、Invoker）。
            // 真正的 publisher/subscribe 注册推迟到 PostConfigureServices，
            // 等所有下游模块通过 services.Configure<EventBusOptions> 把 extension / consumers
            // 堆完之后再一次性 Configure，避免：
            //   ① ConfigureServices 阶段就解析 options
            //   ② options 还没完整就 Configure 一次，下游模块 extension 全部丢失
            context.Services.AddHostedService<EventBusBackgroundService>();
            context.Services.TryAddSingleton<IMessageHandlerInvoker, DefaultMessageHandlerInvoker>();
        }

        /// <summary>
        /// 所有模块的 ConfigureServices 已经跑完，此时 <see cref="EventBusOptions"/> 已聚合完所有扩展。
        /// 这里临时构建 ServiceProvider 仅为解析 options，然后一次性把所有 extension 安装到 IoC。
        /// </summary>
        public override void PostConfigureServices(ServiceCollectionContext context)
        {
            // 短暂构建一个 SP 仅为解析此时已聚合完成的 IOptions<EventBusOptions>。
            // 与原版相比，关键区别是：用 using 显式 Dispose，避免容器及内部 singleton 泄漏；
            // 同时 PostConfigureServices 在所有模块都已 ConfigureServices 之后才跑，
            // 解析到的 options 是真正完整的版本。
            using var serviceProvider = context.Services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<EventBusOptions>>().Value;
            options.Configure(context.Services);
        }
    }
}
