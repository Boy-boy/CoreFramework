using Core.EventBus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Uow
{
    public static class UnitOfWorkServiceCollectionExtensions
    {
        public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        {
            services.TryAddSingleton<IUnitOfWorkAccessor, DefaultUnitOfWorkAccessor>();
            services.TryAddTransient<IUnitOfWorkManager, DefaultUnitOfWorkManager>();
            services.TryAddTransient<IOutboxAmbientContext, UnitOfWorkOutboxAmbientContext>();
            return services;
        }
    }
}
