using Core.Ddd.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.EntityFrameworkCore.Repositories
{
    public interface IEfCoreRepository
    {
    }

    public interface IEfCoreRepository<TEntity>: IEfCoreRepository
        where TEntity : class, IEntity
    {
        DbContext GetDbContext();

        DbSet<TEntity> GetDbSet();
    }
}
