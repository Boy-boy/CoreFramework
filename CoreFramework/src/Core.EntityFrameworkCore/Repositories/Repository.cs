using Core.Ddd.Domain.Entities;
using Core.Ddd.Domain.Repositories;
using Core.EntityFrameworkCore.UnitOfWork;
using Core.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EntityFrameworkCore.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {
        private readonly IUnitOfWork _unitOfWork;
        public DbContext DbContext;
        protected DbSet<TEntity> DbSet;

        public Repository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void InitialDbContext(object dbContext)
        {
            DbContext = dbContext as DbContext ?? throw new Exception("failed to initialize db context,repository could not work without dbContext");
            DbSet = DbContext.Set<TEntity>();
            (_unitOfWork as EfCoreUnitOfWork)?.RegisterCoreDbContext(DbContext);
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

            var query = DbSet.Skip(pageIndex * pageSize).Take(pageSize);
            if (expression != null)
            {
                query = query.Where(expression);
            }
            return (query.ToList(), query.Count());
        }

        public Task<(Task<List<TEntity>> DataQueryable, Task<int>)> PageFindAsync(
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
            var query = DbSet.Skip(pageIndex * pageSize).Take(pageSize);
            if (expression != null)
            {
                query = query.Where(expression);
            }
            return Task.FromResult((query.ToListAsync(cancellationToken), query.CountAsync(cancellationToken)));
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

    public class Repository<TEntity, TKey> : Repository<TEntity>, IRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
    {
        public Repository(IUnitOfWork unitOfWork)
        : base(unitOfWork)
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
            if (entity != null) DbSet.Remove(entity);
        }

        public async Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var entity = await FindAsync(key, cancellationToken);
            if (entity != null) DbSet.Remove(entity);
        }
    }
}
