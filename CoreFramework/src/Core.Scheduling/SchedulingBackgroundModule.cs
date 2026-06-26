using Core.Modularity;
using Core.Modularity.Attribute;
using Core.Scheduling.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Scheduling
{
    /// <summary>
    /// 默认（BackgroundService）调度模块。
    /// 在 <see cref="SchedulingCoreModule"/> 基础上加上 BG 主循环宿主，适合单实例 / 开发 / InMemory。
    /// 集群部署请改用 <c>SchedulingQuartzModule</c> / <c>SchedulingHangfireModule</c>，三者互斥。
    /// </summary>
    [DependsOn(typeof(SchedulingCoreModule))]
    public class SchedulingBackgroundModule : CoreModuleBase
    {
        public SchedulingBackgroundModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            // BG 专属字段绑到独立子节,与 Quartz / Hangfire / Redis / HealthChecks 同级布局;
            // 跨适配器共享的 SchedulingFilterOptions 仍由 SchedulingCoreModule 绑根节 "Scheduling"
            context.Services.Configure<BackgroundSchedulingOptions>(Configuration.GetSection("Scheduling:Background"));
            context.Services.AddHostedService<SchedulerHostedService>();
        }
    }
}
