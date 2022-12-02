using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Uow
{
    public static class UowServiceCollectionExtensions
    {
        public static IServiceCollection AddUowCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IUnitOfWorkAccessor, UnitOfWorkAccessor>();
            return services;
        }
    }
}
