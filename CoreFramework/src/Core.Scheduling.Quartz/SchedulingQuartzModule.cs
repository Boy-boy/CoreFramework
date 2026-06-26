using Core.Modularity;
using Core.Modularity.Attribute;
using Core.Scheduling;
using Core.Scheduling.Options;
using Core.Scheduling.Quartz.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Scheduling.Quartz
{
    /// <summary>
    /// 基于 Quartz.NET 的集群调度模块,与 BG / Hangfire 模块互斥(三选一)。
    /// 配置节:<c>Scheduling</c>(通用) + <c>Scheduling:Quartz</c>(集群专属)。
    /// </summary>
    [DependsOn(typeof(SchedulingCoreModule))]
    public class SchedulingQuartzModule : CoreModuleBase
    {
        public SchedulingQuartzModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            // SchedulingFilterOptions 绑定由 SchedulingCoreModule(DependsOn) 完成;这里只管 Quartz 专属节
            context.Services.Configure<QuartzSchedulingOptions>(Configuration.GetSection("Scheduling:Quartz"));

            // Quartz 内部要在注册期同步读 options,所以这里手动把当前已绑定值拿出来
            var filterOptions = new SchedulingFilterOptions();
            Configuration.GetSection("Scheduling").Bind(filterOptions);
            var quartzOptions = new QuartzSchedulingOptions();
            Configuration.GetSection("Scheduling:Quartz").Bind(quartzOptions);

            context.Services.AddSchedulingQuartz(
                quartz => Copy(quartzOptions, quartz),
                filters => CopyFilters(filterOptions, filters));
        }

        // Quartz 只关心 filter 开关三个字段;BG 专属字段在类型层就拿不到
        private static void CopyFilters(SchedulingFilterOptions from, SchedulingFilterOptions to)
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
