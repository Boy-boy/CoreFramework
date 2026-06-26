using Core.Scheduling.Abstractions;
using Core.Scheduling.Filters;
using Core.Scheduling.Options;
using Core.Scheduling.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Core.Scheduling 共享核 DI 扩展。</summary>
    public static class SchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 注册 Core.Scheduling 抽象与共享基础设施(不含调度宿主)。
        /// BG / Hangfire / Quartz 三个适配器都在自己的 Add 扩展里调用本方法。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="configureFilters">跨适配器共享的 filter 开关(可省),落到独立 <see cref="SchedulingFilterOptions"/> 实例。</param>
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

            services.AddInternalFilters();

            return services;
        }

        /// <summary>
        /// 把一个 <see cref="IScheduledHandler"/> 注册为单例并加入调度管线。
        /// </summary>
        /// <remarks>
        /// 运行时要求 handler 必须是单例(filter / registry 均单例);
        /// 若需 scoped 服务(如 DbContext),请在 <c>ExecuteAsync</c> 内通过
        /// <see cref="HandlerExecutionContext.Services"/> 自建作用域。
        /// </remarks>
        public static IServiceCollection AddScheduledHandler<THandler>(this IServiceCollection services)
            where THandler : class, IScheduledHandler
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            services.AddSingleton<IScheduledHandler, THandler>();
            return services;
        }

        /// <summary>注册一个自定义 <see cref="IHandlerExecutionFilter"/>。</summary>
        public static IServiceCollection AddSchedulingFilter<TFilter>(this IServiceCollection services)
            where TFilter : class, IHandlerExecutionFilter
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHandlerExecutionFilter, TFilter>());
            return services;
        }

        private static void AddInternalFilters(this IServiceCollection services)
        {
            // filter 是否生效由各自 ctor 按 options 决定;此处只做注册(默认全开)
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
