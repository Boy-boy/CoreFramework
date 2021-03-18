using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Core.Ddd.Domain.Entities;

namespace Core.Ddd.Domain.Repositories
{
    public interface IRepository
    {
    }

    public interface IRepository<TEntity> : IRepository
        where TEntity : class, IEntity
    {
        void InitialDbContext(object dbContext);

        IQueryable<TEntity> GetQueryable();

        void Add(IEnumerable<TEntity> entities);

        void Add(TEntity entity);

        Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        long Count(Expression<Func<TEntity, bool>> expression);

        Task<long> CountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

        long Count();
        Task<long> CountAsync(CancellationToken cancellationToken = default);

        TEntity Find(Expression<Func<TEntity, bool>> expression);

        IEnumerable<TEntity> FindAll(Expression<Func<TEntity, bool>> expression);

        Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

        Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

        bool Exists(Expression<Func<TEntity, bool>> expression);

        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

        void Remove(TEntity entity);

        void Remove(IEnumerable<TEntity> entities);

        void Reload(TEntity entity);

        Task ReloadAsync(TEntity entity, CancellationToken cancellationToken = default);

        void Update(TEntity entity);

        (IEnumerable<TEntity> DataQueryable, int Total) PageFind(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>> expression);

        Task<(Task<List<TEntity>> DataQueryable, Task<int>)> PageFindAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>> expression,
             CancellationToken cancellationToken = default);
    }

    public interface IRepository<TEntity, in TKey> : IRepository<TEntity>
        where TEntity : class, IEntity<TKey>
    {
        void Remove(TKey key);

        Task RemoveAsync(TKey key, CancellationToken cancellationToken = default);

        TEntity Find(TKey key);

        Task<TEntity> FindAsync(TKey key, CancellationToken cancellationToken = default);

    }
}
