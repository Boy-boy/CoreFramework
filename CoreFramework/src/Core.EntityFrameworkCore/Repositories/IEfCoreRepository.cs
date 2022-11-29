using Core.Ddd.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Core.EntityFrameworkCore.Repositories
{
    public interface IEfCoreRepository
    {
    }

    public interface IEfCoreRepository<TEntity> : IEfCoreRepository
        where TEntity : class, IEntity
    {
        Task<DbContext> GetDbContextAsync();

        Task<DbSet<TEntity>> GetDbSetAsync();
    }
}
