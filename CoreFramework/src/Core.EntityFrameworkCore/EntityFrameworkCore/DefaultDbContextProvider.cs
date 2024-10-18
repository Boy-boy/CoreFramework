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
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IUnitOfWorkAccessor _unitOfWorkAccessor;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DefaultDbContextProvider(IUnitOfWorkManager unitOfWorkManager,
            IUnitOfWorkAccessor unitOfWorkAccessor,
            IServiceScopeFactory serviceScopeFactory)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _unitOfWorkAccessor = unitOfWorkAccessor;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<TDbContext> GetDbContextAsync()
        {
            var uow = _unitOfWorkManager.Begin();

            var dbContextName = DbContextNameAttribute.GetNameOrDefault(typeof(TDbContext));
            var databaseApi = uow.FindDatabaseApi(dbContextName);
            if (databaseApi == null)
            {
                var dbContext = await CreateDbContextAsync();
                databaseApi = new EfCoreDatabaseApi(dbContext);
                uow.AddDatabaseApi(dbContextName, new EfCoreDatabaseApi(dbContext));
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
    }
}
