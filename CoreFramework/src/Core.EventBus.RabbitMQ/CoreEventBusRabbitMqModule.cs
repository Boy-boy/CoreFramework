using Core.Modularity;
using Core.Modularity.Attribute;
using Core.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.RabbitMQ
{
    [DependsOn(typeof(CoreEventBusModule),
        typeof(CoreRabbitMqModule))]
    public class CoreEventBusRabbitMqModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreEventBusRabbitMqModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AddRabbitMq(Configuration.GetSection("EventBus:RabbitMq"));
            });
        }
    }
}
