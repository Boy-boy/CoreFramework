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
        private readonly IServiceProvider _serviceProvider;

        public DefaultDbContextProvider(IUnitOfWorkManager unitOfWorkManager,
            IServiceProvider serviceProvider)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _serviceProvider = serviceProvider;
        }

        public async Task<TDbContext> GetDbContextAsync()
        {
            var uow = await _unitOfWorkManager.BeginAsync();
            var dbContextName = DbContextNameAttribute.GetNameOrDefault(typeof(TDbContext));

            if (uow.FindDatabaseApi(dbContextName) is EfCoreDatabaseApi existing)
            {
                return (TDbContext)existing.DbContext;
            }

            var dbContext = await CreateDbContextAsync(uow, dbContextName);
            uow.AddDatabaseApi(dbContextName, new EfCoreDatabaseApi(dbContext));
            return dbContext;
        }

        private async Task<TDbContext> CreateDbContextAsync(IUnitOfWork uow, string dbContextName)
        {
            var dbContext = _serviceProvider.GetRequiredService<TDbContext>();

            if (!uow.Options.IsTransactional)
                return dbContext;

            var dbTransaction = uow.Options.IsolationLevel.HasValue
                ? await dbContext.Database.BeginTransactionAsync(uow.Options.IsolationLevel.Value)
                : await dbContext.Database.BeginTransactionAsync();

            uow.AddTransactionApi(dbContextName, new EfCoreTransactionApi(dbTransaction));

            return dbContext;
        }
    }
}
