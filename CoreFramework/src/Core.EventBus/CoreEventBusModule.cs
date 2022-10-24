using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.EventBus
{
    public class CoreEventBusModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddEventBus(_ => { });
        }

        public override void PostConfigureServices(ServiceCollectionContext context)
        {
            var serviceProvider = context.Services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<EventBusOptions>>().Value;
            options.Configure(context.Services);
        }
    }
}
