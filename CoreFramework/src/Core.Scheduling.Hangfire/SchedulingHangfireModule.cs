using Core.Modularity;
using Core.Modularity.Attribute;
using Core.Scheduling;
using Core.Scheduling.Hangfire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchedulingFilterOptions = Core.Scheduling.Hosting.SchedulingFilterOptions;

namespace Core.Scheduling.Hangfire
{
    /// <summary>
    /// 基于 Hangfire 的集群调度模块。
    /// 与 <see cref="SchedulingBackgroundModule"/>(BG) 及 Quartz 模块互斥;消费者只选一个。
    /// 配置节:<c>Scheduling</c>(通用) + <c>Scheduling:Hangfire</c>(Hangfire 专属)。
    /// </summary>
    /// <remarks>
    /// 适用场景:分钟级及以上节奏 + 需要 Hangfire Dashboard。
    /// 秒级节奏请改用 BG 或 Quartz —— Hangfire 适配器在秒级 FixedInterval 上会直接抛 <see cref="System.InvalidOperationException"/>。
    /// </remarks>
    [DependsOn(typeof(SchedulingCoreModule))]
    public class SchedulingHangfireModule : CoreModuleBase
    {
        public SchedulingHangfireModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            // SchedulingFilterOptions 绑定由 SchedulingCoreModule(DependsOn) 完成;这里只管 Hangfire 专属节
            context.Services.Configure<HangfireSchedulingOptions>(Configuration.GetSection("Scheduling:Hangfire"));

            var filterOptions = new SchedulingFilterOptions();
            Configuration.GetSection("Scheduling").Bind(filterOptions);
            var hangfireOptions = new HangfireSchedulingOptions();
            Configuration.GetSection("Scheduling:Hangfire").Bind(hangfireOptions);

            context.Services.AddSchedulingHangfire(
                hangfire => Copy(hangfireOptions, hangfire),
                filters => CopyFilters(filterOptions, filters));
        }

        // Hangfire 模式只关心 filter 开关三个字段;
        // BG 专属字段(IdleDelay/ShutdownGraceTimeout/分布式锁/DefaultMaxBackoff)在这里没意义,在类型层就拿不到
        private static void CopyFilters(SchedulingFilterOptions from, SchedulingFilterOptions to)
        {
            to.EnableTracing = from.EnableTracing;
            to.EnableMetrics = from.EnableMetrics;
            to.EnableLogging = from.EnableLogging;
        }

        private static void Copy(HangfireSchedulingOptions from, HangfireSchedulingOptions to)
        {
            to.PersistenceMode = from.PersistenceMode;
            to.ConnectionString = from.ConnectionString;
            to.SqlServerSchemaName = from.SqlServerSchemaName;
            to.PrepareSchemaIfNecessary = from.PrepareSchemaIfNecessary;
            to.PollingInterval = from.PollingInterval;
            to.JobIdPrefix = from.JobIdPrefix;
            to.CleanupOrphanJobs = from.CleanupOrphanJobs;
            to.NonConcurrentLockTimeoutSeconds = from.NonConcurrentLockTimeoutSeconds;
            to.WorkerCount = from.WorkerCount;
            to.ServerName = from.ServerName;
        }
    }
}
