using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Core.Ddd.Domain.Entities;

namespace Core.Ddd.Domain.Repositories
{
    public class DomainRepository<TAggregateRoot> : IDomainRepository<TAggregateRoot>
       where TAggregateRoot : class, IEntity
    {
        private readonly IRepository<TAggregateRoot> _repository;

        public DomainRepository(IRepository<TAggregateRoot> repository, object dbContext)
        {
            _repository = repository;
            _repository.InitialDbContext(dbContext);
        }

        public void Add(IEnumerable<TAggregateRoot> entities)
        {
            _repository.Add(entities);
        }

        public void Add(TAggregateRoot entity)
        {
            _repository.Add(entity);
        }

        public Task AddAsync(IEnumerable<TAggregateRoot> entities, CancellationToken cancellationToken = default)
        {
            return _repository.AddAsync(entities, cancellationToken);
        }

        public long Count(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.Count(expression);
        }

        public long Count()
        {
            return _repository.Count();
        }

        public Task<long> CountAsync(Expression<Func<TAggregateRoot, bool>> expression, CancellationToken cancellationToken = default)
        {
            return _repository.CountAsync(expression, cancellationToken);
        }

        public Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            return _repository.CountAsync(cancellationToken);
        }

        public bool Exists(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.Exists(expression);
        }

        public Task<bool> ExistsAsync(Expression<Func<TAggregateRoot, bool>> expression, CancellationToken cancellationToken = default)
        {
            return _repository.ExistsAsync(expression, cancellationToken);
        }

        public TAggregateRoot Find(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.Find(expression);
        }

        public IEnumerable<TAggregateRoot> FindAll(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.FindAll(expression);
        }

        public Task<List<TAggregateRoot>> FindAllAsync(Expression<Func<TAggregateRoot, bool>> expression, CancellationToken cancellationToken = default)
        {
            return _repository.FindAllAsync(expression, cancellationToken);
        }

        public Task<TAggregateRoot> FindAsync(Expression<Func<TAggregateRoot, bool>> expression, CancellationToken cancellationToken = default)
        {
            return _repository.FindAsync(expression, cancellationToken);
        }

        public IQueryable<TAggregateRoot> GetQueryable()
        {
            return _repository.GetQueryable();
        }

        public (IEnumerable<TAggregateRoot> DataQueryable, int Total) PageFind(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> expression)
        {
            return _repository.PageFind(pageIndex, pageSize, expression);
        }

        public Task<(Task<List<TAggregateRoot>> DataQueryable, Task<int>)> PageFindAsync(int pageIndex, int pageSize, Expression<Func<TAggregateRoot, bool>> expression, CancellationToken cancellationToken = default)
        {
            return _repository.PageFindAsync(pageIndex, pageSize, expression, cancellationToken);
        }

        public void Reload(TAggregateRoot entity)
        {
            _repository.Reload(entity);
        }

        public Task ReloadAsync(TAggregateRoot entity, CancellationToken cancellationToken = default)
        {
            return _repository.ReloadAsync(entity, cancellationToken);
        }

        public void Remove(TAggregateRoot entity)
        {
            _repository.Remove(entity);
        }

        public void Remove(IEnumerable<TAggregateRoot> entities)
        {
            _repository.Remove(entities);
        }

        public void Update(TAggregateRoot entity)
        {
            _repository.Update(entity);
        }
    }

    public class DomainRepository<TAggregateRoot, TKey> : DomainRepository<TAggregateRoot>, IDomainRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IEntity<TKey>
    {
        private readonly IRepository<TAggregateRoot, TKey> _repository;

        public DomainRepository(IRepository<TAggregateRoot, TKey> repository,object dbContext)
        : base(repository, dbContext)
        {
            _repository = repository;
        }

        public TAggregateRoot Find(TKey key)
        {
            return _repository.Find(key);
        }

        public Task<TAggregateRoot> FindAsync(TKey key, CancellationToken cancellationToken = default)
        {
            return _repository.FindAsync(key, cancellationToken);
        }

        public void Remove(TKey key)
        {
            _repository.Remove(key);
        }

        public Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            return _repository.RemoveAsync(key, cancellationToken);
        }
    }
}
