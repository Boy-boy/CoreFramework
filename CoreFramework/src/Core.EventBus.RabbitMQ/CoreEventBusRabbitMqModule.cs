using Core.Modularity;
using Core.Modularity.Attribute;
using Core.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.RabbitMQ
{
    [DependsOn(typeof(CoreEventBusModule), typeof(CoreRabbitMqModule))]
    public class CoreEventBusRabbitMqModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddEventBusRabbitMq();
        }
    }
}
