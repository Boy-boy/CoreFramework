using Core.Modularity;
using Core.Modularity.Attribute;
using Core.Scheduling;
using Core.Scheduling.Hangfire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchedulingOptions = Core.Scheduling.Hosting.SchedulingOptions;
using SharedSchedulingOptions = Core.Scheduling.Hosting.SharedSchedulingOptions;

namespace Core.Scheduling.Hangfire
{
    /// <summary>
    /// 基于 Hangfire 的集群调度模块。
    /// 与 <see cref="CoreSchedulingModule"/>(BG) 及 Quartz 模块互斥;消费者只选一个。
    /// 配置节:<c>Scheduling</c>(通用) + <c>Scheduling:Hangfire</c>(Hangfire 专属)。
    /// </summary>
    /// <remarks>
    /// 适用场景:分钟级及以上节奏 + 需要 Hangfire Dashboard。
    /// 秒级节奏请改用 BG 或 Quartz —— Hangfire 适配器在秒级 FixedInterval 上会直接抛 <see cref="System.InvalidOperationException"/>。
    /// </remarks>
    [DependsOn(typeof(CoreSchedulingCoreModule))]
    public class CoreSchedulingHangfireModule : CoreModuleBase
    {
        public CoreSchedulingHangfireModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<SchedulingOptions>(Configuration.GetSection("Scheduling"));
            context.Services.Configure<HangfireSchedulingOptions>(Configuration.GetSection("Scheduling:Hangfire"));

            var schedulingOptions = new SchedulingOptions();
            Configuration.GetSection("Scheduling").Bind(schedulingOptions);
            var hangfireOptions = new HangfireSchedulingOptions();
            Configuration.GetSection("Scheduling:Hangfire").Bind(hangfireOptions);

            context.Services.AddCoreSchedulingHangfire(
                hangfire => Copy(hangfireOptions, hangfire),
                scheduling => CopyShared(schedulingOptions, scheduling));
        }

        // Hangfire 模式只关心 Shared 字段(三个 filter 开关);
        // IdleDelay/ShutdownGraceTimeout/分布式锁/DefaultMaxBackoff 等 BG 专属字段在这里没意义
        private static void CopyShared(SchedulingOptions from, SharedSchedulingOptions to)
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
