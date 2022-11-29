using System;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Core.EntityFrameworkCore.EntityFrameworkCore;
using Core.Uow;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EntityFrameworkCore
{
    public class DefaultDbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
    where TDbContext : DbContext
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IServiceProvider _serviceProvider;

        public DefaultDbContextProvider(IUnitOfWork unitOfWork,
            IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _serviceProvider = serviceProvider;
        }

        public Task<TDbContext> GetDbContextAsync()
        {
            var dbContextName = DbContextNameAttribute.GetNameOrDefault(typeof(TDbContext));
            var databaseApi = (EfCoreDatabaseApi)_unitOfWork.GetOrAddDatabaseApi(dbContextName, () => new EfCoreDatabaseApi(CreateDbContext()));
            return Task.FromResult((TDbContext)databaseApi.DbContext);
        }

        private TDbContext CreateDbContext()
        {
            return _serviceProvider.GetRequiredService<TDbContext>();
        }
    }
}
