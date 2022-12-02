using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Local
{
    public class EventBusOptionsExtensions : IEventBusOptionsExtensions
    {
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<ILocalMessagePublisher, LocalMessagePublisher>();
            services.TryAddSingleton<ILocalMessageSubscribe, LocalMessageSubscribe>();
            services.TryAddSingleton<ILocalMessageHandlerManager, LocalMessageHandlerManager>();
            services.TryAddSingleton<ILocalMessageHandlerProvider, LocalMessageHandlerProvider>();
        }
    }
}
