using Core.Scheduling.Abstractions;
using Core.Scheduling.Filters;
using Core.Scheduling.Hosting;
using Core.Scheduling.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Core.Scheduling 依赖注入扩展。
    /// </summary>
    public static class SchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 注册 Core.Scheduling 的抽象与共享基础设施（不含调度宿主本身）。
        /// BG / Hangfire / Quartz 三个适配器都会在自己的 Add 扩展里调用本方法。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="configureFilters">
        /// 跨适配器共享的 filter 开关配置(可省)。落到独立的 <see cref="SchedulingFilterOptions"/> 实例,
        /// 与 BG 专属的 <see cref="SchedulingOptions"/> 解耦——后者只在 <c>AddSchedulingBackground</c>(BG) 路径下注册。
        /// </param>
        public static IServiceCollection AddSchedulingCore(
            this IServiceCollection services,
            Action<SchedulingFilterOptions> configureFilters = null)
        {
            services.AddOptions<SchedulingFilterOptions>();
            if (configureFilters != null)
                services.Configure(configureFilters);

            services.TryAddSingleton(TimeProvider.System);

            services.TryAddSingleton<HandlerStateStore>();
            services.TryAddSingleton<IHandlerExecutionInspector>(sp =>
                sp.GetRequiredService<HandlerStateStore>());

            services.TryAddSingleton<IScheduledHandlerRegistry, ScheduledHandlerRegistry>();

            services.TryAddSingleton<HandlerExecutionPipeline>();
            services.TryAddSingleton<IHandlerExecutionPipeline>(sp =>
                sp.GetRequiredService<HandlerExecutionPipeline>());

            // 内置过滤器按需启用，可通过 SchedulingFilterOptions.Enable* 关闭。
            // INextRunStrategy 不在共享层注册——只有 BG 才算下次时间,
            // Hangfire/Quartz 由自家引擎决定下次,DI 容器里不出现这个接口。
            services.AddInternalFilters();

            return services;
        }

        /// <summary>
        /// 注册基于 <see cref="Microsoft.Extensions.Hosting.BackgroundService"/> 的调度宿主。
        /// 与 <c>AddSchedulingHangfire</c> / <c>AddSchedulingQuartz</c> 互斥,三选一。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="configureOptions">BG 专属字段(IdleDelay、停机等待、退避兜底、分布式锁)。可省。</param>
        /// <param name="configureFilters">
        /// 跨适配器共享的 filter 开关(Tracing/Metrics/Logging)。可省。
        /// 与 BG 字段分两个 Options 类型,各自独立绑定 / 热更新。
        /// </param>
        public static IServiceCollection AddSchedulingBackground(
            this IServiceCollection services,
            Action<SchedulingOptions> configureOptions = null,
            Action<SchedulingFilterOptions> configureFilters = null)
        {
            services.AddOptions<SchedulingOptions>();
            if (configureOptions != null)
                services.Configure(configureOptions);

            services.AddSchedulingCore(configureFilters);

            // 默认 noop 锁;集群部署时由 Core.Scheduling.Redis 等包通过 Replace 替换。
            // 只有 BG 宿主消费本接口,Hangfire/Quartz 路径不注册以免误以为它在生效。
            services.TryAddSingleton<IDistributedHandlerLock, NoopDistributedHandlerLock>();

            // BG 模式负责自己派发,需要算真实的下次时间(含退避)
            services.TryAddSingleton<INextRunStrategy, BackgroundNextRunStrategy>();
            // BG 专属 filter:在 StateTrackingFilter 之后读状态、算 next-run 并写回 HandlerStateStore
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHandlerExecutionFilter, BackgroundNextRunFilter>());

            services.AddHostedService<SchedulerHostedService>();
            return services;
        }

        /// <summary>
        /// 把一个 <see cref="IScheduledHandler"/> 注册为单例并加入调度管线。
        /// 注：调度运行时要求 handler 必须是单例（filter/registry 均为单例）；
        /// 若需 scoped 服务（如 DbContext），请在 <c>ExecuteAsync</c> 内通过
        /// <see cref="HandlerExecutionContext.Services"/> 自建作用域。
        /// </summary>
        public static IServiceCollection AddScheduledHandler<THandler>(this IServiceCollection services)
            where THandler : class, IScheduledHandler
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            services.AddSingleton<IScheduledHandler, THandler>();
            return services;
        }

        /// <summary>
        /// 注册一个自定义 <see cref="IHandlerExecutionFilter"/>。
        /// </summary>
        public static IServiceCollection AddSchedulingFilter<TFilter>(this IServiceCollection services)
            where TFilter : class, IHandlerExecutionFilter
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHandlerExecutionFilter, TFilter>());
            return services;
        }

        private static void AddInternalFilters(this IServiceCollection services)
        {
            // 这些 filter 是否生效由其 ctor 在运行时按 options 决定;此处直接注册.
            // 关闭时:见各 filter 中读取 options 的逻辑(默认全开).
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHandlerExecutionFilter, TracingExecutionFilter>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHandlerExecutionFilter, LoggingExecutionFilter>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHandlerExecutionFilter, MetricsExecutionFilter>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHandlerExecutionFilter, StateTrackingFilter>());
        }
    }
}
