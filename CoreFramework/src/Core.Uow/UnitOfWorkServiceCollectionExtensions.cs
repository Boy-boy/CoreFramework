using Microsoft.Extensions.DependencyInjection;

namespace Core.Uow
{
    public static class UnitOfWorkServiceCollectionExtensions
    {
        public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        {
            services.AddSingleton<IUnitOfWorkAccessor, DefaultUnitOfWorkAccessor>();
            services.AddTransient(provider => provider.GetRequiredService<IUnitOfWorkAccessor>().UnitOfWork);
            return services;
        }
    }
}
