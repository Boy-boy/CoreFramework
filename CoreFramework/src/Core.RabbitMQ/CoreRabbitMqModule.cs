using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Core.RabbitMQ
{
    public class CoreRabbitMqModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddRabbitMq();
        }
    }
}
