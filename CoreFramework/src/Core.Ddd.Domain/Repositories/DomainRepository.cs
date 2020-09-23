using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Core.Ddd.Domain.Repositories
{
    public class DomainRepository<TAggregateRoot> : IDomainRepository<TAggregateRoot>
       where TAggregateRoot : class
    {
        private readonly IRepository<TAggregateRoot> _repository;

        public DomainRepository(IRepository<TAggregateRoot> repository)
        {
            _repository = repository;
        }

        public void Add(IEnumerable<TAggregateRoot> entities)
        {
            _repository.Add(entities);
        }

        public void Add(TAggregateRoot entity)
        {
            _repository.Add(entity);
        }

        public Task AddAsync(IEnumerable<TAggregateRoot> entities)
        {
            return _repository.AddAsync(entities);
        }

        public TAggregateRoot GetByKey(params object[] keyValues)
        {
            return _repository.GetByKey(keyValues);
        }

        public ValueTask<TAggregateRoot> GetByKeyAsync(params object[] keyValues)
        {
            return _repository.GetByKeyAsync(keyValues);
        }

        public long Count(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.Count(expression);
        }

        public Task<long> CountAsync(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.CountAsync(expression);
        }

        public long Count()
        {
            return _repository.Count();
        }

        public Task<long> CountAsync()
        {
            return _repository.CountAsync();
        }

        public IQueryable<TAggregateRoot> FindAll(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.FindAll(expression);
        }

        public TAggregateRoot Find(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.Find(expression);
        }

        public Task<TAggregateRoot> FindAsync(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.FindAsync(expression);
        }

        public bool Exists(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.Exists(expression);
        }

        public Task<bool> ExistsAsync(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.ExistsAsync(expression);
        }

        public void Remove(TAggregateRoot entity)
        {
            _repository.Remove(entity);
        }

        public void Remove(IEnumerable<TAggregateRoot> entities)
        {
            _repository.Remove(entities);
        }

        public void Reload(TAggregateRoot entity)
        {
            _repository.Reload(entity);
        }

        public Task ReloadAsync(TAggregateRoot entity)
        {
            return _repository.ReloadAsync(entity);
        }

        public void Update(TAggregateRoot entity)
        {
            _repository.Update(entity);
        }

        public (IQueryable<TAggregateRoot> DataQueryable, long Total) PageFind(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.PageFind(pageIndex, pageSize, expression);
        }

        public Task<(IQueryable<TAggregateRoot> DataQueryable, Task<int>)> PageFindAsync(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.PageFindAsync(pageIndex, pageSize, expression);
        }
    }
}
