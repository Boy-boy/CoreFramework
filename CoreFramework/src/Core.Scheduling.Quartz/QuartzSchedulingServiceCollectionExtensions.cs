using System;
using Core.Scheduling.Options;
using Core.Scheduling.Quartz.Hosting;
using Core.Scheduling.Quartz.Jobs;
using Core.Scheduling.Quartz.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using global::Quartz;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Core.Scheduling.Quartz 依赖注入扩展。</summary>
    public static class QuartzSchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 注册基于 Quartz.NET 的集群调度宿主。
        /// 自动调用 <c>AddSchedulingCore</c> 引入共享抽象与内置 filter,与 <c>AddSchedulingBackground</c> 互斥。
        /// </summary>
        /// <remarks>
        /// <paramref name="configureQuartz"/> 必填(集群存储 / 连接串无合理默认);
        /// <paramref name="configureFilters"/> 可省——3 个 filter 开关默认全开。
        /// 后者只接 <see cref="SchedulingFilterOptions"/>(BG 专属字段在 Quartz 模式无意义,编译期就拦住)。
        /// <paramref name="configureQuartz"/> 注册期同步调用一次,回调里的 I/O 或日志不会双触发。
        /// </remarks>
        /// <param name="services">服务集合。</param>
        /// <param name="configureQuartz">Quartz / 集群专属配置(必填)。</param>
        /// <param name="configureFilters">跨适配器共享的 filter 开关,省略走默认值。</param>
        public static IServiceCollection AddSchedulingQuartz(
            this IServiceCollection services,
            Action<QuartzSchedulingOptions> configureQuartz,
            Action<SchedulingFilterOptions> configureFilters = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureQuartz == null) throw new ArgumentNullException(nameof(configureQuartz));

            // filter 回调透传到共享层,落到独立 SchedulingFilterOptions 实例(与 BG 解耦)
            services.AddSchedulingCore(configureFilters);

            // 回调跑一次:先解出 options 用于 AddQuartz(同步),再用同一份注册到 IOptions
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
