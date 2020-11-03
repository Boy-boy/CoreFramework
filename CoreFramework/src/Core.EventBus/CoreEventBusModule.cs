using Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.EventBus
{
    public class CoreEventBusModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            var options = context.Services.BuildServiceProvider().GetService<IOptions<EventBusOptions>>().Value;
            context.Services
                .AddEventBus()
                .AutoRegistrarHandlers(options.AutoRegistrarHandlersAssemblies);
        }

        public override void Configure(ApplicationBuilderContext context)
        {
            context.ApplicationBuilder.UseEventBus();
        }
    }
}
