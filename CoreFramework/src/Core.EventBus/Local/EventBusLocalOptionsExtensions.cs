using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Local
{
    public class EventBusLocalOptionsExtensions : IEventBusOptionsExtensions
    {
        public void AddServices(IServiceCollection services)
        {
            services.TryAddSingleton<IMessagePublisher, LocalMessagePublisher>();
            services.TryAddSingleton<IMessageSubscribe, LocalMessageSubscribe>();
        }
    }
}
