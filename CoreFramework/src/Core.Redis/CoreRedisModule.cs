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
            context.Services
                .AddRedisCache()
                .Configure<RedisCacheOptions>(_configuration.GetSection("Redis"));
        }
    }
}
