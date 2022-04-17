using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;

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
            context.Items.TryGetValue(nameof(EventBusBuilder), out var eventBusBuilder);
            ((EventBusBuilder)eventBusBuilder).AddPostgreSql(Configuration.GetSection("EventBus:Storage"));
        }
    }
}
