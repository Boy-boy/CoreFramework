using Core.Ddd.Domain.Entities;
using Core.Ddd.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Core.EntityFrameworkCore.UnitOfWork;
using Core.Uow;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EntityFrameworkCore.Repositories
{
    public class EfCoreRepository<TDbContext, TEntity> : IRepository<TEntity>,
        IEfCoreRepository<TEntity>
        where TDbContext : DbContext
        where TEntity : class, IEntity
    {
        private readonly DbContext _dbContext;
        private readonly DbSet<TEntity> _dbSet;

        public EfCoreRepository(IServiceProvider serviceProvider, IUnitOfWorkManager unitOfWorkManager)
        {
            var dbContextName = DbContextNameAttribute.GetNameOrDefault(typeof(TDbContext));
            if (!unitOfWorkManager.TryGetUnitOfWork(dbContextName, out var unitOfWork))
            {
                var unitOfWorkType = typeof(EfCoreUnitOfWork<>).MakeGenericType(typeof(TDbContext));
                unitOfWork = (IUnitOfWork)ActivatorUtilities.CreateInstance(serviceProvider, unitOfWorkType);
                unitOfWorkManager.TryAddUnitOfWork(dbContextName, unitOfWork);
            }
            var unitOfWork1 = (EfCoreUnitOfWork<TDbContext>)unitOfWork;
            _dbContext = unitOfWork1.DbContext;
            _dbSet = unitOfWork1.DbContext.Set<TEntity>();
        }

        public DbContext GetDbContext()
        {
            return _dbContext;
        }

        public DbSet<TEntity> GetDbSet()
        {
            return _dbSet;
        }

        public IQueryable<TEntity> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }

        public void Add(TEntity entity)
        {
            _dbSet.Add(entity);
        }

        public void Add(IEnumerable<TEntity> entities)
        {
            var enumerable = entities as TEntity[] ?? entities.ToArray();
            _dbSet.AddRange(enumerable);
        }

        public Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            return _dbSet.AddRangeAsync(entities, cancellationToken);
        }

        public long Count(Expression<Func<TEntity, bool>> expression)
        {
            return _dbSet.LongCount(expression);
        }

        public long Count()
        {
            return _dbSet.LongCount();
        }

        public Task<long> CountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            return _dbSet.LongCountAsync(expression, cancellationToken);
        }

        public Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            return _dbSet.LongCountAsync(cancellationToken);
        }

        public bool Exists(Expression<Func<TEntity, bool>> expression)
        {
            return _dbSet.Any(expression);
        }

        public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            return _dbSet.AnyAsync(expression, cancellationToken);
        }

        public TEntity Find(Expression<Func<TEntity, bool>> expression)
        {
            return _dbSet.Where(expression).FirstOrDefault();
        }

        public IEnumerable<TEntity> FindAll(Expression<Func<TEntity, bool>> expressions)
        {
            return _dbSet.Where(expressions).ToList();
        }

        public Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            return _dbSet.Where(expression).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            return _dbSet.Where(expression).ToListAsync(cancellationToken);
        }

        public (IEnumerable<TEntity> DataQueryable, int Total) PageFind(
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

            var query = _dbSet.AsQueryable();
            if (expression != null)
            {
                query = query.Where(expression);
            }

            var total = query.Count();
            var list = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            return (list, total);
        }

        public async Task<(IEnumerable<TEntity> DataQueryable, int)> PageFindAsync(
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
            var query = _dbSet.AsQueryable();
            if (expression != null)
            {
                query = query.Where(expression);
            }
            var total = await query.CountAsync(cancellationToken);
            var list = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return await Task.FromResult((list, total));
        }

        public (IEnumerable<TEntity> DataQueryable, int Total) PageFind(
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

        public async Task<(IEnumerable<TEntity> DataQueryable, int)> PageFindAsync(
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
            _dbContext.Entry(entity).Reload();
        }

        public Task ReloadAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return _dbContext.Entry(entity)
                .ReloadAsync(cancellationToken);
        }

        public void Remove(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public void Remove(IEnumerable<TEntity> entities)
        {
            var enumerable = entities as TEntity[] ?? entities.ToArray();
            _dbSet.RemoveRange(enumerable);
        }

        public void Update(TEntity entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
        }
    }

    public class EfCoreRepository<TDbContext, TEntity, TKey> : EfCoreRepository<TDbContext, TEntity>, IRepository<TEntity, TKey>
        where TDbContext : DbContext
        where TEntity : class, IEntity<TKey>
    {
        public EfCoreRepository(IServiceProvider serviceProvider, IUnitOfWorkManager unitOfWorkManager)
        : base(serviceProvider, unitOfWorkManager)
        {
        }

        public TEntity Find(TKey key)
        {
            return GetDbSet().FirstOrDefault(p => p.Id.Equals(key));
        }

        public Task<TEntity> FindAsync(TKey key, CancellationToken cancellationToken = default)
        {
            return GetDbSet().FirstOrDefaultAsync(p => p.Id.Equals(key), cancellationToken);
        }

        public void Remove(TKey key)
        {
            var entity = Find(key);
            if (entity != null)
            {
                GetDbSet().Remove(entity);
            }
        }

        public async Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var entity = await FindAsync(key, cancellationToken);
            if (entity != null)
            {
                GetDbSet().Remove(entity);
            }
        }
    }
}
