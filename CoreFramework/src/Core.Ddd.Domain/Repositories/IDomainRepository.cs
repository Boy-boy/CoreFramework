using Core.Ddd.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Ddd.Domain.Repositories
{
    public interface IDomainRepository
    {
    }

    public interface IDomainRepository<TAggregateRoot>
        where TAggregateRoot : class, IEntity
    {
        IQueryable<TAggregateRoot> GetQueryable();

        void Add(IEnumerable<TAggregateRoot> entities);

        void Add(TAggregateRoot entity);

        Task AddAsync(IEnumerable<TAggregateRoot> entities, CancellationToken cancellationToken = default);

        long Count(Expression<Func<TAggregateRoot, bool>> expression);

        Task<long> CountAsync(Expression<Func<TAggregateRoot, bool>> expression, CancellationToken cancellationToken = default);

        long Count();
        Task<long> CountAsync(CancellationToken cancellationToken = default);

        TAggregateRoot Find(Expression<Func<TAggregateRoot, bool>> expression);

        IEnumerable<TAggregateRoot> FindAll(Expression<Func<TAggregateRoot, bool>> expression);

        Task<TAggregateRoot> FindAsync(Expression<Func<TAggregateRoot, bool>> expression, CancellationToken cancellationToken = default);

        Task<List<TAggregateRoot>> FindAllAsync(Expression<Func<TAggregateRoot, bool>> expression, CancellationToken cancellationToken = default);

        bool Exists(Expression<Func<TAggregateRoot, bool>> expression);

        Task<bool> ExistsAsync(Expression<Func<TAggregateRoot, bool>> expression, CancellationToken cancellationToken = default);

        void Remove(TAggregateRoot entity);

        void Remove(IEnumerable<TAggregateRoot> entities);

        void Reload(TAggregateRoot entity);

        Task ReloadAsync(TAggregateRoot entity, CancellationToken cancellationToken = default);

        void Update(TAggregateRoot entity);

        (IEnumerable<TAggregateRoot> DataQueryable, int Total) PageFind(
            int pageIndex,
            int pageSize,
            Expression<Func<TAggregateRoot, bool>> expression);

        Task<(Task<List<TAggregateRoot>> DataQueryable, Task<int>)> PageFindAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TAggregateRoot, bool>> expression,
            CancellationToken cancellationToken = default);
    }

    public interface IDomainRepository<TEntity, in TKey> : IDomainRepository<TEntity>
        where TEntity : class, IEntity<TKey>
    {
        void Remove(TKey key);

        Task RemoveAsync(TKey id, CancellationToken cancellationToken = default);

        TEntity Find(TKey key);

        Task<TEntity> FindAsync(TKey key, CancellationToken cancellationToken = default);

    }
}
