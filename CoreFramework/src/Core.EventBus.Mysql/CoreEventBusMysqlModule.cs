using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Mysql
{
    [DependsOn(typeof(CoreEventBusModule))]
    public class CoreEventBusMysqlModule : CoreModuleBase
    {
        private readonly IConfiguration _configuration;

        public CoreEventBusMysqlModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusMysqlOptions>(_configuration.GetSection("EventBus:Storage"));
            context.Items.TryGetValue(nameof(EventBusBuilder), out var eventBusBuilder);
            ((EventBusBuilder)eventBusBuilder).AddMysql();
        }
    }
}
