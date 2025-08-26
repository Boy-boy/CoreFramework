using Core.EntityFrameworkCore.EntityFrameworkCore;
using Core.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EntityFrameworkCore
{
    public class DefaultDbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
    where TDbContext : DbContext
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IUnitOfWorkAccessor _unitOfWorkAccessor;
        private readonly IServiceProvider _serviceProvider;

        public DefaultDbContextProvider(IUnitOfWorkManager unitOfWorkManager,
            IUnitOfWorkAccessor unitOfWorkAccessor,
            IServiceProvider serviceProvider)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _unitOfWorkAccessor = unitOfWorkAccessor;
            _serviceProvider = serviceProvider;
        }

        public async Task<TDbContext> GetDbContextAsync()
        {
            var uow = await _unitOfWorkManager.BeginAsync();

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
            var dbContext = _serviceProvider.GetRequiredService<TDbContext>();

            var uow = _unitOfWorkAccessor.UnitOfWork;
            if (uow.Options.IsTransactional)
            {
                var dbTransaction = uow.Options.IsolationLevel.HasValue
                    ? await dbContext.Database.BeginTransactionAsync(uow.Options.IsolationLevel.Value)
                    : await dbContext.Database.BeginTransactionAsync();

                var dbContextName = DbContextNameAttribute.GetNameOrDefault(typeof(TDbContext));
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
