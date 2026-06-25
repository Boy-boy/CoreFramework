using Core.Modularity;
using Core.Modularity.Attribute;
using Core.Scheduling;
using Core.Scheduling.Quartz.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchedulingOptions = Core.Scheduling.Hosting.SchedulingOptions;
using SharedSchedulingOptions = Core.Scheduling.Hosting.SharedSchedulingOptions;

namespace Core.Scheduling.Quartz
{
    /// <summary>
    /// 基于 Quartz.NET 的集群调度模块。
    /// 与 <see cref="CoreSchedulingModule"/> 互斥;消费者只在一个应用里挑一个。
    /// 配置节:<c>Scheduling</c>(通用) + <c>Scheduling:Quartz</c>(集群专属)。
    /// </summary>
    [DependsOn(typeof(CoreSchedulingCoreModule))]
    public class CoreSchedulingQuartzModule : CoreModuleBase
    {
        public CoreSchedulingQuartzModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<SchedulingOptions>(Configuration.GetSection("Scheduling"));
            context.Services.Configure<QuartzSchedulingOptions>(Configuration.GetSection("Scheduling:Quartz"));

            // 用 IConfiguration 已 Configure 过的 options 触发 AddCoreSchedulingQuartz,
            // 但 Quartz 内部要在注册期同步读 options,所以这里手动把当前已绑定值拿出来
            var schedulingOptions = new SchedulingOptions();
            Configuration.GetSection("Scheduling").Bind(schedulingOptions);
            var quartzOptions = new QuartzSchedulingOptions();
            Configuration.GetSection("Scheduling:Quartz").Bind(quartzOptions);

            context.Services.AddCoreSchedulingQuartz(
                quartz => Copy(quartzOptions, quartz),
                scheduling => CopyShared(schedulingOptions, scheduling));
        }

        // Quartz 模式只关心 Shared 字段(三个 filter 开关);
        // IdleDelay/ShutdownGraceTimeout/分布式锁/DefaultMaxBackoff 等 BG 专属字段在这里没意义
        private static void CopyShared(SchedulingOptions from, SharedSchedulingOptions to)
        {
            to.EnableTracing = from.EnableTracing;
            to.EnableMetrics = from.EnableMetrics;
            to.EnableLogging = from.EnableLogging;
        }

        private static void Copy(QuartzSchedulingOptions from, QuartzSchedulingOptions to)
        {
            to.PersistenceMode = from.PersistenceMode;
            to.ConnectionString = from.ConnectionString;
            to.SchedulerName = from.SchedulerName;
            to.InstanceId = from.InstanceId;
            to.TablePrefix = from.TablePrefix;
            to.ClusterEnabled = from.ClusterEnabled;
            to.ClusterCheckinInterval = from.ClusterCheckinInterval;
            to.ThreadCount = from.ThreadCount;
            to.JobGroup = from.JobGroup;
            to.CleanupOrphanJobs = from.CleanupOrphanJobs;
        }
    }
}
