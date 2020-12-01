using Core.Ddd.Domain.Repositories;
using Core.EntityFrameworkCore.Repositories;
using Core.EntityFrameworkCore.UnitOfWork;
using Core.Uow;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EfCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkRepository(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.TryAddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            services.TryAddScoped(typeof(IUnitOfWork), typeof(EfCoreUnitOfWork));
            return services;
        }
    }
}
