using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;

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
            context.Items.TryGetValue(nameof(EventBusBuilder), out var eventBusBuilder);
            ((EventBusBuilder)eventBusBuilder).AddSqlServer(Configuration.GetSection("EventBus:Storage"));
        }
    }
}
