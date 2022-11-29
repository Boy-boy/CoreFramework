using Core.Ddd.Domain.Entities;
using Core.Ddd.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EntityFrameworkCore.Repositories
{
    public class EfCoreRepository<TDbContext, TEntity> : IRepository<TEntity>,
        IEfCoreRepository<TEntity>
        where TDbContext : DbContext
        where TEntity : class, IEntity
    {
        private readonly IDbContextProvider<TDbContext> _dbContextProvider;

        protected TDbContext DbContext => (TDbContext)GetDbContextAsync().Result;

        protected DbSet<TEntity> DbSet => GetDbSetAsync().Result;

        public EfCoreRepository(IDbContextProvider<TDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        public async Task<DbContext> GetDbContextAsync()
        {
            return await _dbContextProvider.GetDbContextAsync();
        }

        public async Task<DbSet<TEntity>> GetDbSetAsync()
        {
            return (await GetDbContextAsync()).Set<TEntity>();
        }

        public IQueryable<TEntity> GetQueryable()
        {
            return DbSet.AsQueryable();
        }

        public void Add(TEntity entity)
        {
            DbSet.Add(entity);
        }

        public void Add(IEnumerable<TEntity> entities)
        {
            var enumerable = entities as TEntity[] ?? entities.ToArray();
            DbSet.AddRange(enumerable);
        }

        public Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            return DbSet.AddRangeAsync(entities, cancellationToken);
        }

        public long Count(Expression<Func<TEntity, bool>> expression)
        {
            return DbSet.LongCount(expression);
        }

        public long Count()
        {
            return DbSet.LongCount();
        }

        public Task<long> CountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            return DbSet.LongCountAsync(expression, cancellationToken);
        }

        public Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            return DbSet.LongCountAsync(cancellationToken);
        }

        public bool Exists(Expression<Func<TEntity, bool>> expression)
        {
            return DbSet.Any(expression);
        }

        public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            return DbSet.AnyAsync(expression, cancellationToken);
        }

        public TEntity Find(Expression<Func<TEntity, bool>> expression)
        {
            return DbSet.Where(expression).FirstOrDefault();
        }

        public IEnumerable<TEntity> FindAll(Expression<Func<TEntity, bool>> expressions)
        {
            return DbSet.Where(expressions).ToList();
        }

        public Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            return DbSet.Where(expression).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            return DbSet.Where(expression).ToListAsync(cancellationToken);
        }

        public (IEnumerable<TEntity> DataEnumerable, int Total) PageFind(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>> expression)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("InvalidPageIndex");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("InvalidPageCount");
            }

            var query = DbSet.AsQueryable();
            if (expression != null)
            {
                query = query.Where(expression);
            }

            var total = query.Count();
            var list = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            return (list, total);
        }

        public async Task<(IEnumerable<TEntity> DataEnumerable, int Total)> PageFindAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>> expression,
            CancellationToken cancellationToken = default)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("InvalidPageIndex");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("InvalidPageCount");
            }
            var query = DbSet.AsQueryable();
            if (expression != null)
            {
                query = query.Where(expression);
            }
            var total = await query.CountAsync(cancellationToken);
            var list = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return await Task.FromResult((list, total));
        }

        public (IEnumerable<TEntity> DataEnumerable, int Total) PageFind(
             int pageIndex,
             int pageSize,
             IQueryable<TEntity> queryable)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("InvalidPageIndex");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("InvalidPageCount");
            }
            var total = queryable.Count();
            var list = queryable.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            return (list, total);
        }

        public async Task<(IEnumerable<TEntity> DataEnumerable, int Total)> PageFindAsync(
              int pageIndex,
              int pageSize,
              IQueryable<TEntity> queryable,
              CancellationToken cancellationToken = default)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("InvalidPageIndex");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("InvalidPageCount");
            }
            var total = await queryable.CountAsync(cancellationToken);
            var list = await queryable.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return await Task.FromResult((list, total));
        }

        public void Reload(TEntity entity)
        {
            DbContext.Entry(entity).Reload();
        }

        public Task ReloadAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return DbContext.Entry(entity)
                .ReloadAsync(cancellationToken);
        }

        public void Remove(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        public void Remove(IEnumerable<TEntity> entities)
        {
            var enumerable = entities as TEntity[] ?? entities.ToArray();
            DbSet.RemoveRange(enumerable);
        }

        public void Update(TEntity entity)
        {
            DbContext.Entry(entity).State = EntityState.Modified;
        }
    }

    public class EfCoreRepository<TDbContext, TEntity, TKey> : EfCoreRepository<TDbContext, TEntity>, IRepository<TEntity, TKey>
        where TDbContext : DbContext
        where TEntity : class, IEntity<TKey>
    {
        public EfCoreRepository(IDbContextProvider<TDbContext> dbContextProvider)
        : base(dbContextProvider)
        {
        }

        public TEntity Find(TKey key)
        {
            return DbSet.FirstOrDefault(p => p.Id.Equals(key));
        }

        public Task<TEntity> FindAsync(TKey key, CancellationToken cancellationToken = default)
        {
            return DbSet.FirstOrDefaultAsync(p => p.Id.Equals(key), cancellationToken);
        }

        public void Remove(TKey key)
        {
            var entity = Find(key);
            if (entity != null)
            {
                DbSet.Remove(entity);
            }
        }

        public async Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var entity = await FindAsync(key, cancellationToken);
            if (entity != null)
            {
                DbSet.Remove(entity);
            }
        }
    }
}
