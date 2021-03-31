using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.PostgreSql
{
    [DependsOn(typeof(CoreEventBusModule))]
    public class CoreEventBusPostgreSqlModule : CoreModuleBase
    {
        private readonly IConfiguration _configuration;

        public CoreEventBusPostgreSqlModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusPostgreSqlOptions>(_configuration.GetSection("EventBus:Storage"));
            context.Items.TryGetValue(nameof(EventBusBuilder), out var eventBusBuilder);
            ((EventBusBuilder)eventBusBuilder).AddPostgreSql();
        }
    }
}
