using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Core.Ddd.Domain.Repositories
{
    public interface IRepository
    {
    }
    public interface IRepository<TAggregateRoot> : IRepository
        where TAggregateRoot : class
    {

        void Add(IEnumerable<TAggregateRoot> entities);


        void Add(TAggregateRoot entity);


        Task AddAsync(IEnumerable<TAggregateRoot> entities);

        TAggregateRoot GetByKey(params object[] keyValues);

        ValueTask<TAggregateRoot> GetByKeyAsync(params object[] keyValues);


        long Count(Expression<Func<TAggregateRoot, bool>> expression);

        Task<long> CountAsync(Expression<Func<TAggregateRoot, bool>> expression);

        long Count();
        Task<long> CountAsync();

        IQueryable<TAggregateRoot> FindAll(Expression<Func<TAggregateRoot, bool>> expression);

        TAggregateRoot Find(Expression<Func<TAggregateRoot, bool>> expression);

        Task<TAggregateRoot> FindAsync(Expression<Func<TAggregateRoot, bool>> expression);

        bool Exists(Expression<Func<TAggregateRoot, bool>> expression);
        Task<bool> ExistsAsync(Expression<Func<TAggregateRoot, bool>> expression);

        void Remove(TAggregateRoot entity);

        void Remove(IEnumerable<TAggregateRoot> entities);

        void Reload(TAggregateRoot entity);
        Task ReloadAsync(TAggregateRoot entity);

        void Update(TAggregateRoot entity);

        (IQueryable<TAggregateRoot> DataQueryable, long Total) PageFind(int pageIndex,
            int pageSize,
            Expression<Func<TAggregateRoot, bool>> expression);

        Task<(IQueryable<TAggregateRoot> DataQueryable, Task<int>)> PageFindAsync(int pageIndex,
            int pageSize,
            Expression<Func<TAggregateRoot, bool>> expression);


    }
}
