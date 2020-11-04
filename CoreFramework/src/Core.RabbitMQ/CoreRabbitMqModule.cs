using Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.RabbitMQ
{
    public class CoreRabbitMqModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreRabbitMqModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<RabbitMqOptions>(Configuration.GetSection("RabbitMq"));
            context.Services.AddRabbitMq();
        }
    }
}
