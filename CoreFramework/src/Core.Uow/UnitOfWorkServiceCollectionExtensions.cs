using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Uow
{
    public static class UnitOfWorkServiceCollectionExtensions
    {
        public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        {
            services.TryAddSingleton<IUnitOfWorkAccessor, DefaultUnitOfWorkAccessor>();
            services.TryAddSingleton<IUnitOfWorkManager, DefaultUnitOfWorkManager>();
            services.TryAddTransient(provider => provider.GetRequiredService<IUnitOfWorkAccessor>().UnitOfWork);
            return services;
        }
    }
}
