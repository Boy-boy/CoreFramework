using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Local
{
    [DependsOn(typeof(CoreEventBusModule))]
    public class CoreEventBusLocalModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AddLocalMq();
            });
        }
    }
}
