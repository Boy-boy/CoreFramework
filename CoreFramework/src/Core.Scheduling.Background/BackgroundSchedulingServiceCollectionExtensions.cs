using Core.Scheduling.Abstractions;
using Core.Scheduling.DistributedLocking;
using Core.Scheduling.Hosting;
using Core.Scheduling.NextRun;
using Core.Scheduling.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Core.Scheduling.Background 依赖注入扩展。</summary>
    public static class BackgroundSchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 注册基于 <see cref="Microsoft.Extensions.Hosting.BackgroundService"/> 的调度宿主。
        /// 与 <c>AddSchedulingHangfire</c> / <c>AddSchedulingQuartz</c> 互斥,三选一。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="configureOptions">BG 专属字段(IdleDelay、停机等待、退避兜底、分布式锁),可省。</param>
        /// <param name="configureFilters">跨适配器共享的 filter 开关(Tracing/Metrics/Logging),可省,与 BG 字段分两个 Options 独立绑定 / 热更新。</param>
        public static IServiceCollection AddSchedulingBackground(
            this IServiceCollection services,
            Action<BackgroundSchedulingOptions> configureOptions = null,
            Action<SchedulingFilterOptions> configureFilters = null)
        {
            services.AddOptions<BackgroundSchedulingOptions>();
            if (configureOptions != null)
                services.Configure(configureOptions);

            services.AddSchedulingCore(configureFilters);

            // 默认 noop 锁;集群部署由 Core.Scheduling.Redis 等包通过 Replace 替换。
            // 只 BG 宿主消费,Hangfire / Quartz 路径不注册以免误以为它在生效。
            services.TryAddSingleton<IDistributedHandlerLock, NoopDistributedHandlerLock>();

            // BG 自行派发,需要算真实的下次时间(含退避)
            services.TryAddSingleton<INextRunStrategy, BackgroundNextRunStrategy>();
            // BG 专属 filter:在 StateTrackingFilter 之后读状态算 next-run 写回
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHandlerExecutionFilter, BackgroundNextRunFilter>());

            services.AddHostedService<SchedulerHostedService>();
            return services;
        }
    }
}
