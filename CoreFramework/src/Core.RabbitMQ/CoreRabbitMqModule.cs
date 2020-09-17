using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Core.RabbitMQ
{
    public class CoreRabbitMqModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            var configuration = context.Services.GetConfiguration();
            context.Services.Configure<RabbitMqOptions>(configuration?.GetSection("RabbitMq"));
            context.Services.AddRabbitMq();
        }
    }
}
