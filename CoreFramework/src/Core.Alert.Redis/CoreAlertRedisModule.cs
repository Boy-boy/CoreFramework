using Core.Alert;
using Core.Modularity;
using Core.Modularity.Attribute;
using Core.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Alert.Redis
{
    /// <summary>
    /// Redis 存储适配模块。
    /// 通过配置 Alert:Storage，将默认内存存储替换为 Redis 实现。
    /// </summary>
    [DependsOn(typeof(CoreAlertModule), typeof(CoreRedisModule))]
    public class CoreAlertRedisModule : CoreModuleBase
    {
        public CoreAlertRedisModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<AlertOptions>(options =>
            {
                options.AddRedis(Configuration.GetSection("Alert:Storage"));
            });
        }
    }
}
