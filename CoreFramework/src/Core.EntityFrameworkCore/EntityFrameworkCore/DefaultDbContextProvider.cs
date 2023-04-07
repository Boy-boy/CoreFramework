using Core.EntityFrameworkCore.EntityFrameworkCore;
using Core.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Core.EntityFrameworkCore
{
    public class DefaultDbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
    where TDbContext : DbContext
    {
        private readonly IUnitOfWorkAccessor _unitOfWorkAccessor;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DefaultDbContextProvider(IUnitOfWorkAccessor unitOfWorkAccessor,
            IServiceScopeFactory serviceScopeFactory)
        {
            _unitOfWorkAccessor = unitOfWorkAccessor;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<TDbContext> GetDbContextAsync()
        {
            var uow = _unitOfWorkAccessor.UnitOfWork;
            if (uow == null)
            {
                uow = (_unitOfWorkAccessor.UnitOfWork = CreateUnitOfWork());
            }

            var dbContextName = DbContextNameAttribute.GetNameOrDefault(typeof(TDbContext));

            var databaseApi = uow.FindDatabaseApi(dbContextName);
            if (databaseApi == null)
            {
                var dbContext = await CreateDbContextAsync();
                databaseApi = uow.GetOrAddDatabaseApi(dbContextName, () => new EfCoreDatabaseApi(dbContext));
            }

            return (TDbContext)((EfCoreDatabaseApi)databaseApi).DbContext;
        }

        private async Task<TDbContext> CreateDbContextAsync()
        {
            var dbContextName = DbContextNameAttribute.GetNameOrDefault(typeof(TDbContext));
            var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

            var uow = _unitOfWorkAccessor.UnitOfWork;
            if (uow.Options.IsTransactional)
            {
                var dbTransaction = uow.Options.IsolationLevel.HasValue
                    ? await dbContext.Database.BeginTransactionAsync(uow.Options.IsolationLevel.Value)
                    : await dbContext.Database.BeginTransactionAsync();

                uow.AddTransactionApi(
                    dbContextName,
                    new EfCoreTransactionApi(
                        dbTransaction
                    )
                );
            }

            return dbContext;
        }

        private IUnitOfWork CreateUnitOfWork()
        {
            var scope = _serviceScopeFactory.CreateScope();
            var uow = ActivatorUtilities.CreateInstance<DefaultUnitOfWork>(scope.ServiceProvider);
            uow.Initialize(new UnitOfWorkOptions());
            return uow;
        }
    }
}
