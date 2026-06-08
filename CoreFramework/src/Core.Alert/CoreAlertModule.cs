using Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Alert
{
    /// <summary>
    /// 告警升级模块主入口。
    /// 绑定 "Alert" 配置段，并注册默认的内存状态存储实现。
    /// </summary>
    public class CoreAlertModule : CoreModuleBase
    {
        public CoreAlertModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<AlertOptions>(Configuration.GetSection("Alert"));
            context.Services.AddAlertEscalation();
        }

        public override void PostConfigureServices(ServiceCollectionContext context)
        {
            var serviceProvider = context.Services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<AlertOptions>>().Value;
            options.Configure(context.Services);
        }
    }
}
