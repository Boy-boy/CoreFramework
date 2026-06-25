using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Scheduling.HealthChecks
{
    /// <summary>
    /// Core.Scheduling 健康检查模块。
    /// 通过 <c>[DependsOn(typeof(SchedulingCoreModule))]</c> 拉入抽象 + 状态检视;
    /// 与 BG / Quartz / Hangfire 宿主模块均可共存,业务侧再叠加任一种触发宿主即可。
    /// 配置节:<c>Scheduling:HealthChecks</c>。
    /// </summary>
    [DependsOn(typeof(SchedulingCoreModule))]
    public class SchedulingHealthChecksModule : CoreModuleBase
    {
        public SchedulingHealthChecksModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<SchedulingHealthCheckOptions>(
                Configuration.GetSection("Scheduling:HealthChecks"));

            context.Services.AddSchedulingHealthCheck();
        }
    }
}
