using Core.Uow;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static  class UowServiceCollectionExtensions
    {
        public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(IUnitOfWorkManager), typeof(UnitOfWorkManager));
            return services;
        }
    }
}
