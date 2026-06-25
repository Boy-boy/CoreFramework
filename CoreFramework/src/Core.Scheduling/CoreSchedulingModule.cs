using Core.Modularity;
using Core.Modularity.Attribute;
using Core.Scheduling.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Scheduling
{
    /// <summary>
    /// 默认（BackgroundService）调度模块。
    /// 在 <see cref="CoreSchedulingCoreModule"/> 基础上加上 BG 主循环宿主，适合单实例 / 开发 / InMemory。
    /// 集群部署请改用 <c>CoreSchedulingQuartzModule</c>，二者互斥。
    /// </summary>
    [DependsOn(typeof(CoreSchedulingCoreModule))]
    public class CoreSchedulingModule : CoreModuleBase
    {
        public CoreSchedulingModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddHostedService<SchedulerHostedService>();
        }
    }
}
