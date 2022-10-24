using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Mysql
{
    [DependsOn(typeof(CoreEventBusModule))]
    public class CoreEventBusMysqlModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreEventBusMysqlModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusOptions>(options =>
                {
                    options.AddMysql(Configuration.GetSection("EventBus:Storage"));
                });
        }
    }
}
