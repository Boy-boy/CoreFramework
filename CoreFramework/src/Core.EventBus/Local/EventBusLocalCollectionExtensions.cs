using Core.EventBus.Abstraction;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Local
{
    public static class EventBusLocalCollectionExtensions
    {
        public static IServiceCollection AddLocal(this IServiceCollection services)
        {
            services.AddSingleton<IEventBus, EventBusLocal>();
            return services;
        }
    }
}
