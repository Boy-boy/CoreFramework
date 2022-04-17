using System.Linq;
using Core.Modularity;
using Core.Modularity.Attribute;
using Core.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.EventBus.RabbitMQ
{
    [DependsOn(
        typeof(CoreEventBusModule),
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
            context.Items.TryGetValue(nameof(EventBusBuilder), out var eventBusBuilder);
            ((EventBusBuilder)eventBusBuilder).AddRabbitMq(Configuration.GetSection("EventBus:RabbitMq"));
        }

        public override void PostConfigureServices(ServiceCollectionContext context)
        {
            //TODO:此处为了解决使用端直接注入IConfigureOptions<EventBusRabbitMqOptions>对象而导致RabbitMqConnection属性不起作用
            var implementationInstances = context.Services
                 .Where(p => p.ServiceType == typeof(IConfigureOptions<EventBusRabbitMqOptions>))
                 .Select(p => (IConfigureOptions<EventBusRabbitMqOptions>)p.ImplementationInstance)
                 .ToList();

            if (!implementationInstances.Any())
                return;

            var eventBusRabbitMqOptions = new EventBusRabbitMqOptions();
            foreach (var implementationInstance in implementationInstances)
            {
                implementationInstance.Configure(eventBusRabbitMqOptions);
            }

            context.Services.AddRabbitMq(options =>
            {
                options.Connection = eventBusRabbitMqOptions.Connection;
            });
        }
    }
}
