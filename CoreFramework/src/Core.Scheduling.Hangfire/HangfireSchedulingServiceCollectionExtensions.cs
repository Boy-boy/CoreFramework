using System;
using Core.Scheduling.Hangfire.Hosting;
using Core.Scheduling.Hangfire.Jobs;
using Core.Scheduling.Hangfire.Options;
using Core.Scheduling.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using global::Hangfire;
using global::Hangfire.InMemory;
using global::Hangfire.SqlServer;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Core.Scheduling.Hangfire 依赖注入扩展。</summary>
    public static class HangfireSchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 注册基于 Hangfire 的集群调度宿主。
        /// 自动调用 <c>AddSchedulingCore</c> 引入共享抽象与内置 filter,
        /// 与 <c>AddSchedulingBackground</c> / <c>AddSchedulingQuartz</c> 互斥。
        /// </summary>
        /// <remarks>
        /// <paramref name="configureHangfire"/> 必填(存储模式 / 连接串无合理默认);
        /// <paramref name="configureFilters"/> 可省——3 个 filter 开关默认全开。
        /// 后者只接 <see cref="SchedulingFilterOptions"/>(BG 专属字段在 Hangfire 模式无意义,编译期就拦住)。
        /// <paramref name="configureHangfire"/> 注册期同步调用一次,回调里的 I/O 或日志不会双触发。
        /// </remarks>
        /// <param name="services">服务集合。</param>
        /// <param name="configureHangfire">Hangfire 专属配置(必填)。</param>
        /// <param name="configureFilters">跨适配器共享的 filter 开关,省略走默认值。</param>
        public static IServiceCollection AddSchedulingHangfire(
            this IServiceCollection services,
            Action<HangfireSchedulingOptions> configureHangfire,
            Action<SchedulingFilterOptions> configureFilters = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureHangfire == null) throw new ArgumentNullException(nameof(configureHangfire));

            // filter 回调透传到共享层,落到独立 SchedulingFilterOptions 实例(与 BG 解耦)
            services.AddSchedulingCore(configureFilters);

            // 回调跑一次,先解出 options 给 AddHangfire/AddHangfireServer(同步),再复制到 IOptions
            var hangfireOptions = new HangfireSchedulingOptions();
            configureHangfire(hangfireOptions);

            services.Configure<HangfireSchedulingOptions>(o => CopyOptions(hangfireOptions, o));

            services.TryAddTransient<ScheduledHandlerJobInvoker>();

            services.AddHangfire(config =>
            {
                ConfigureStorage(config, hangfireOptions);
            });

            services.AddHangfireServer(opts =>
            {
                opts.SchedulePollingInterval = hangfireOptions.PollingInterval < TimeSpan.FromSeconds(1)
                    ? TimeSpan.FromSeconds(1)
                    : hangfireOptions.PollingInterval;

                if (hangfireOptions.WorkerCount is { } workers && workers > 0)
                    opts.WorkerCount = workers;

                if (!string.IsNullOrWhiteSpace(hangfireOptions.ServerName))
                    opts.ServerName = hangfireOptions.ServerName;
            });

            services.AddHostedService<HangfireSchedulerBootstrapHostedService>();

            return services;
        }

        private static void CopyOptions(HangfireSchedulingOptions src, HangfireSchedulingOptions dst)
        {
            dst.PersistenceMode = src.PersistenceMode;
            dst.ConnectionString = src.ConnectionString;
            dst.SqlServerSchemaName = src.SqlServerSchemaName;
            dst.PrepareSchemaIfNecessary = src.PrepareSchemaIfNecessary;
            dst.PollingInterval = src.PollingInterval;
            dst.JobIdPrefix = src.JobIdPrefix;
            dst.CleanupOrphanJobs = src.CleanupOrphanJobs;
            dst.NonConcurrentLockTimeoutSeconds = src.NonConcurrentLockTimeoutSeconds;
            dst.WorkerCount = src.WorkerCount;
            dst.ServerName = src.ServerName;
        }

        private static void ConfigureStorage(IGlobalConfiguration config, HangfireSchedulingOptions opts)
        {
            switch (opts.PersistenceMode)
            {
                case HangfirePersistenceMode.InMemory:
                    config.UseInMemoryStorage();
                    return;

                case HangfirePersistenceMode.SqlServer:
                    if (string.IsNullOrWhiteSpace(opts.ConnectionString))
                        throw new InvalidOperationException(
                            "HangfireSchedulingOptions.ConnectionString is required when PersistenceMode = SqlServer.");

                    config.UseSqlServerStorage(opts.ConnectionString, new SqlServerStorageOptions
                    {
                        SchemaName = opts.SqlServerSchemaName,
                        PrepareSchemaIfNecessary = opts.PrepareSchemaIfNecessary,
                        // 其余采用 Hangfire 推荐默认
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    });
                    return;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported HangfirePersistenceMode: {opts.PersistenceMode}.");
            }
        }
    }
}
