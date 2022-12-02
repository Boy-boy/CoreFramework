using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Ddd.Domain.Events
{
    public static class DomainEventServiceCollectionExtensions
    {
        public static IServiceCollection AddDomainEventBus(this IServiceCollection services)
        {
            services.TryAddScoped<IDomainEventBus, DomainEventBus>();
            return services;
        }
    }
}
