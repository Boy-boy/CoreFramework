using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Integration
{
    public static class IntegrationServiceCollectionExtensions
    {
        public static IServiceCollection AddIntegrationCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IIntegrationMessageHandlerManager, IntegrationMessageHandlerManager>();
            services.TryAddSingleton<IIntegrationMessageHandlerProvider, IntegrationMessageHandlerProvider>();
            return services;
        }
    }
}
