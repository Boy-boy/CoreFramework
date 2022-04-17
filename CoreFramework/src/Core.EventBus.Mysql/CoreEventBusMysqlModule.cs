using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;

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
            context.Items.TryGetValue(nameof(EventBusBuilder), out var eventBusBuilder);
            ((EventBusBuilder)eventBusBuilder).AddMysql(Configuration.GetSection("EventBus:Storage"));
        }
    }
}
