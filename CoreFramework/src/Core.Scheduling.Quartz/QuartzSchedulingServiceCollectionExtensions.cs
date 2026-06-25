using System;
using Core.Scheduling.Quartz.Hosting;
using Core.Scheduling.Quartz.Jobs;
using Microsoft.Extensions.DependencyInjection.Extensions;
using global::Quartz;
using SharedSchedulingOptions = Core.Scheduling.Hosting.SharedSchedulingOptions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Core.Scheduling.Quartz 依赖注入扩展。
    /// </summary>
    public static class QuartzSchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 注册基于 Quartz.NET 的集群调度宿主。
        /// 会自动调用 <c>AddCoreSchedulingCore</c> 引入共享抽象 + 内置 filter,
        /// 但**不**注册默认 BG 宿主,与 <c>AddCoreScheduling</c> 互斥。
        /// <para>
        /// <paramref name="configureQuartz"/> 必填(集群存储 / 连接串等没有合理默认);
        /// <paramref name="configureScheduling"/> 可省 —— <see cref="SharedSchedulingOptions"/>
        /// 的 3 个 filter 开关默认全开,Quartz 场景下多数情况无需调整。
        /// 想关掉某个 filter 再传入。
        /// </para>
        /// <para>
        /// <paramref name="configureScheduling"/> 只接 <see cref="SharedSchedulingOptions"/>:
        /// BG 专属的 IdleDelay / 分布式锁等字段在 Quartz 模式下无意义,故编译期就拦住。
        /// </para>
        /// <para>
        /// <paramref name="configureQuartz"/> 在注册期仅同步调用一次,回调中如有 I/O 或日志不会被双触发。
        /// </para>
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="configureQuartz">Quartz/集群专属配置(必填)。</param>
        /// <param name="configureScheduling">跨适配器共享的调度配置(filter 开关);省略走默认值。</param>
        public static IServiceCollection AddCoreSchedulingQuartz(
            this IServiceCollection services,
            Action<QuartzSchedulingOptions> configureQuartz,
            Action<SharedSchedulingOptions> configureScheduling = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureQuartz == null) throw new ArgumentNullException(nameof(configureQuartz));

            // SchedulingOptions 继承 SharedSchedulingOptions,把 Shared 回调(可能为 null)透传给 Core 注册即可
            services.AddCoreSchedulingCore(configureScheduling);

            // 回调只跑一次:先解出 options 用于 AddQuartz(同步),再用同一份注册到 IOptions
            var quartzOptions = new QuartzSchedulingOptions();
            configureQuartz(quartzOptions);

            services.Configure<QuartzSchedulingOptions>(o => CopyOptions(quartzOptions, o));

            // 显式注册两种 Job 类型供 DI 实例化,避免依赖 ActivatorUtilities 隐式回落
            services.TryAddTransient<ScheduledHandlerJob>();
            services.TryAddTransient<ConcurrentScheduledHandlerJob>();

            services.AddQuartz(q =>
            {
                q.SchedulerName = quartzOptions.SchedulerName;
                q.SchedulerId = quartzOptions.InstanceId;
                q.UseDefaultThreadPool(tp => tp.MaxConcurrency = quartzOptions.ThreadCount);
                ConfigurePersistence(q, quartzOptions);
            });

            services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);
            services.AddHostedService<QuartzSchedulerBootstrapHostedService>();

            return services;
        }

        private static void CopyOptions(QuartzSchedulingOptions src, QuartzSchedulingOptions dst)
        {
            dst.PersistenceMode = src.PersistenceMode;
            dst.ConnectionString = src.ConnectionString;
            dst.SchedulerName = src.SchedulerName;
            dst.InstanceId = src.InstanceId;
            dst.TablePrefix = src.TablePrefix;
            dst.ClusterEnabled = src.ClusterEnabled;
            dst.ClusterCheckinInterval = src.ClusterCheckinInterval;
            dst.ThreadCount = src.ThreadCount;
            dst.JobGroup = src.JobGroup;
            dst.CleanupOrphanJobs = src.CleanupOrphanJobs;
        }

        private static void ConfigurePersistence(IServiceCollectionQuartzConfigurator q, QuartzSchedulingOptions opts)
        {
            switch (opts.PersistenceMode)
            {
                case QuartzPersistenceMode.InMemory:
                    // 默认即 RAMJobStore,无需配置
                    return;

                case QuartzPersistenceMode.SqlServer:
                    if (string.IsNullOrWhiteSpace(opts.ConnectionString))
                        throw new InvalidOperationException(
                            "QuartzSchedulingOptions.ConnectionString is required when PersistenceMode = SqlServer.");

                    q.UsePersistentStore(s =>
                    {
                        s.UseProperties = true;
                        s.UseSystemTextJsonSerializer();
                        s.UseSqlServer(c =>
                        {
                            c.ConnectionString = opts.ConnectionString;
                            c.TablePrefix = opts.TablePrefix;
                        });
                        if (opts.ClusterEnabled)
                            s.UseClustering(c => c.CheckinInterval = opts.ClusterCheckinInterval);
                    });
                    return;

                case QuartzPersistenceMode.PostgreSql:
                    if (string.IsNullOrWhiteSpace(opts.ConnectionString))
                        throw new InvalidOperationException(
                            "QuartzSchedulingOptions.ConnectionString is required when PersistenceMode = PostgreSql.");

                    q.UsePersistentStore(s =>
                    {
                        s.UseProperties = true;
                        s.UseSystemTextJsonSerializer();
                        s.UsePostgres(c =>
                        {
                            c.ConnectionString = opts.ConnectionString;
                            c.TablePrefix = opts.TablePrefix;
                        });
                        if (opts.ClusterEnabled)
                            s.UseClustering(c => c.CheckinInterval = opts.ClusterCheckinInterval);
                    });
                    return;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported QuartzPersistenceMode: {opts.PersistenceMode}.");
            }
        }
    }
}
