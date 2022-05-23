using System.Linq;
using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.EventBus
{
    public class CoreEventBusModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            var eventBusBuilder = context.Services.AddEventBus(_ => { });
            context.Items.Add(nameof(EventBusBuilder), eventBusBuilder);
        }

        public override void PostConfigureServices(ServiceCollectionContext context)
        {
            //TODO:兼容客户端注入EventBusOptions
            var implementationInstances = context.Services
                .Where(p => p.ServiceType == typeof(IConfigureOptions<EventBusOptions>))
                .Select(p => (IConfigureOptions<EventBusOptions>)p.ImplementationInstance)
                .ToList();

            if (!implementationInstances.Any())
                return;

            var eventBusOptions = new EventBusOptions();
            foreach (var implementationInstance in implementationInstances)
            {
                implementationInstance.Configure(eventBusOptions);
            }
            eventBusOptions.Configure(context.Services);
        }
    }
}
