using Core.Ddd.Domain.Repositories;
using Core.EntityFrameworkCore.Repositories;
using Core.EntityFrameworkCore.UnitOfWork;
using Core.Modularity;
using Core.Uow;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EntityFrameworkCore
{
    public class CoreEfCoreModule:CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.TryAddScoped(typeof(IRepository<>), typeof(Repository<>));
            context.Services.TryAddScoped(typeof(IUnitOfWork), typeof(EfCoreUnitOfWork));
        }
    }
}
