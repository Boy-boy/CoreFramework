using Core.Ddd.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Core.EntityFrameworkCore.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity>
        where TEntity : class
    {
        private readonly DbContext _dbContext;
        private readonly DbSet<TEntity> _dbSet;

        public Repository(DbContext dbContext)
        {
            _dbContext = dbContext ?? throw new Exception("repository could not work without dbContext");
            _dbSet = dbContext.Set<TEntity>();
        }
        public void Add(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }
        }

        public void Add(TEntity entity)
        {
            _dbSet.Add(entity);
        }

        public Task AddAsync(IEnumerable<TEntity> entities)
        {
            return _dbSet.AddRangeAsync(entities);
        }

        public long Count(Expression<Func<TEntity, bool>> expression)
        {
            return _dbSet.LongCount(expression);
        }

        public long Count()
        {
            return _dbSet.LongCount();
        }

        public Task<long> CountAsync(Expression<Func<TEntity, bool>> expression)
        {
            return _dbSet.LongCountAsync(expression);
        }

        public Task<long> CountAsync()
        {
            return _dbSet.LongCountAsync();
        }

        public bool Exists(Expression<Func<TEntity, bool>> expression)
        {
            return _dbSet.Any(expression);
        }

        public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> expression)
        {
            return _dbSet.AnyAsync(expression);
        }

        public TEntity Find(Expression<Func<TEntity, bool>> expression)
        {
            return _dbSet.Where(expression).FirstOrDefault();
        }

        public IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>> expressions)
        {
            return _dbSet.Where(expressions);
        }

        public Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> expression)
        {
            return _dbSet.Where(expression).FirstOrDefaultAsync();
        }

        public TEntity GetByKey(params object[] keyValues)
        {
            return _dbSet.Find(keyValues);
        }

        public ValueTask<TEntity> GetByKeyAsync(params object[] keyValues)
        {
            return _dbSet.FindAsync(keyValues);
        }

        public (IQueryable<TEntity> DataQueryable, long Total) PageFind(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> expression)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("InvalidPageIndex");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("InvalidPageCount");
            }

            var query = _dbSet.Skip(pageIndex * pageSize).Take(pageSize);
            if (expression != null)
            {
                query = query.Where(expression);
            }
            return (query, query.Count());
        }

        public Task<(IQueryable<TEntity> DataQueryable, Task<int>)> PageFindAsync(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> expression)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("InvalidPageIndex");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("InvalidPageCount");
            }
            var query = _dbSet.Skip(pageIndex * pageSize).Take(pageSize);
            if (expression != null)
            {
                query = query.Where(expression);
            }
            return Task.FromResult((query, query.CountAsync()));
        }

        public void Reload(TEntity entity)
        {
            _dbContext.Entry(entity).Reload();
        }

        public Task ReloadAsync(TEntity entity)
        {
            return _dbContext.Entry(entity)
                .ReloadAsync();
        }

        public void Remove(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public void Remove(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Remove(entity);
            }
        }

        public void Update(TEntity entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
        }
    }
}
