using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus
{
    public class CoreEventBusModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddEventBus();
        }
    }
}
