using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.SqlServer
{
    [DependsOn(typeof(CoreEventBusModule))]
    public class CoreEventBusSqlServerModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreEventBusSqlServerModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AddSqlServer(Configuration.GetSection("EventBus:Storage"));
            });
        }
    }
}
