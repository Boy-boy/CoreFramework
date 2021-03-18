using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus
{
    public class CoreEventBusModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            var eventBusBuilder = context.Services.AddEventBus();
            context.Items.Add(nameof(EventBusBuilder), eventBusBuilder);
        }
    }
}
