using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.PostgreSql
{
    [DependsOn(typeof(CoreEventBusModule))]
    public class CoreEventBusPostgreSqlModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreEventBusPostgreSqlModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AddPostgreSql(Configuration.GetSection("EventBus:Storage"));
            });
        }
    }
}
