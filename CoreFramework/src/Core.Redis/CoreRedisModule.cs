using Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Redis
{
    public class CoreRedisModule : CoreModuleBase
    {
        private readonly IConfiguration _configuration;

        public CoreRedisModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            // 单例注册 IRedisCache → StackExchangeRedis；再绑定 "Redis" 配置段。
            // 调用方若想覆盖，可在 Module 之后继续 services.Configure<RedisCacheOptions>(...)，后注册者胜出。
            context.Services
                .AddRedisCache()
                .Configure<RedisCacheOptions>(_configuration.GetSection("Redis"));
        }
    }
}
