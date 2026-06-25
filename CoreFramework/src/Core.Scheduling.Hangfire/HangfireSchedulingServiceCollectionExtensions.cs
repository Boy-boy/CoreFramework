using System;
using Core.Scheduling.Hangfire.Hosting;
using Core.Scheduling.Hangfire.Jobs;
using Microsoft.Extensions.DependencyInjection.Extensions;
using global::Hangfire;
using global::Hangfire.InMemory;
using global::Hangfire.SqlServer;
using SharedSchedulingOptions = Core.Scheduling.Hosting.SharedSchedulingOptions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Core.Scheduling.Hangfire 依赖注入扩展。
    /// </summary>
    public static class HangfireSchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 注册基于 Hangfire 的集群调度宿主。
        /// 自动调用 <c>AddCoreSchedulingCore</c> 引入共享抽象与内置 filter,
        /// 与 <c>AddCoreScheduling</c>(BG)/<c>AddCoreSchedulingQuartz</c> 互斥。
        /// <para>
        /// <paramref name="configureHangfire"/> 必填(存储模式 / 连接串等没有合理默认);
        /// <paramref name="configureScheduling"/> 可省 —— <see cref="SharedSchedulingOptions"/>
        /// 的 3 个 filter 开关默认全开,Hangfire 场景下多数情况无需调整。
        /// 想关掉某个 filter 再传入。
        /// </para>
        /// <para>
        /// <paramref name="configureScheduling"/> 只接 <see cref="SharedSchedulingOptions"/>:
        /// BG 专属的 IdleDelay / 分布式锁等字段在 Hangfire 模式下无意义,故编译期就拦住。
        /// </para>
        /// <para>
        /// <paramref name="configureHangfire"/> 在注册期仅同步调用一次,回调中如有 I/O 或日志不会被双触发。
        /// </para>
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="configureHangfire">Hangfire 专属配置(必填)。</param>
        /// <param name="configureScheduling">跨适配器共享的调度配置(filter 开关);省略走默认值。</param>
        public static IServiceCollection AddCoreSchedulingHangfire(
            this IServiceCollection services,
            Action<HangfireSchedulingOptions> configureHangfire,
            Action<SharedSchedulingOptions> configureScheduling = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureHangfire == null) throw new ArgumentNullException(nameof(configureHangfire));

            // SchedulingOptions 继承 SharedSchedulingOptions,把 Shared 回调(可能为 null)透传给 Core 注册即可
            services.AddCoreSchedulingCore(configureScheduling);

            // 回调只跑一次,先解出 options 给 AddHangfire/AddHangfireServer(同步),再复制到 IOptions
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
                        // 其余采用 Hangfire 推荐默认:
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
