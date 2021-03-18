using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.SqlServer
{
    [DependsOn(typeof(CoreEventBusModule))]
    public class CoreEventBusSqlServerModule : CoreModuleBase
    {
        private readonly IConfiguration _configuration;

        public CoreEventBusSqlServerModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusSqlServerOptions>(_configuration.GetSection("EventBus:Storage"));
            context.Items.TryGetValue(nameof(EventBusBuilder), out var eventBusBuilder);
            ((EventBusBuilder)eventBusBuilder).AddSqlServer();
        }
    }
}
