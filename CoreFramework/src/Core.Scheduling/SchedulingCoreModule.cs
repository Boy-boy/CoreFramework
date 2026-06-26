using Core.Modularity;
using Core.Scheduling.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Scheduling
{
    /// <summary>
    /// Core.Scheduling 抽象层模块:只注册公共抽象(注册表、管线、状态检视、内置 filter),不带任何宿主。
    /// 各适配器模块(BG / Quartz / ...)通过 <c>[DependsOn(typeof(SchedulingCoreModule))]</c> 引入。
    /// </summary>
    public class SchedulingCoreModule : CoreModuleBase
    {
        public SchedulingCoreModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            // 共享层只绑跨宿主的 filter 开关;BG 专属字段由 SchedulingBackgroundModule 负责
            context.Services.Configure<SchedulingFilterOptions>(Configuration.GetSection("Scheduling"));
            context.Services.AddSchedulingCore();
        }
    }
}
