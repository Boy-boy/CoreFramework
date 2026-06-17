using Core.Ddd.Domain.Entities;
using Core.Ddd.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Core.EntityFrameworkCore.Repositories
{
    public class EfCoreRepository<TDbContext, TEntity> : IRepository<TEntity>,
        IEfCoreRepository<TEntity>,
        IBulkRepository<TEntity>
        where TDbContext : DbContext
        where TEntity : class, IEntity
    {
        private readonly IDbContextProvider<TDbContext> _dbContextProvider;

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
            var dbContext = await GetDbContextAsync();
            return dbContext.Set<TEntity>();
        }

        public async Task<IQueryable<TEntity>> GetQueryableAsync()
        {
            var dbSet = await GetDbSetAsync();
            return dbSet.AsQueryable();
        }

        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            await dbSet.AddAsync(entity, cancellationToken);
        }

        public async Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            await dbSet.AddRangeAsync(entities, cancellationToken);
        }

        public async Task<long> CountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.LongCountAsync(expression, cancellationToken);
        }

        public async Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.LongCountAsync(cancellationToken);
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.AnyAsync(expression, cancellationToken);
        }

        public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.AnyAsync(cancellationToken);
        }

        public async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(expression).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TEntity> FindAsync(CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.SingleOrDefaultAsync(expression, cancellationToken);
        }

        public async Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(expression).ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<TEntity> DataEnumerable, long Total)> GetPagedListAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>> expression,
            CancellationToken cancellationToken = default)
        {
            if (pageIndex < 1)
            {
                throw new ArgumentException("InvalidPageIndex");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("InvalidPageCount");
            }

            var dbSet = await GetDbSetAsync();
            IQueryable<TEntity> query = dbSet;
            if (expression != null)
            {
                query = query.Where(expression);
            }

            var total = await query.LongCountAsync(cancellationToken);
            var list = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return (list, total);
        }

        public async Task<(IEnumerable<TEntity> DataEnumerable, long Total)> GetPagedListAsync(
              int pageIndex,
              int pageSize,
              IQueryable<TEntity> queryable,
              CancellationToken cancellationToken = default)
        {
            if (pageIndex < 1)
            {
                throw new ArgumentException("InvalidPageIndex");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("InvalidPageCount");
            }
            var total = await queryable.LongCountAsync(cancellationToken);
            var list = await queryable.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return (list, total);
        }

        public async Task ReloadAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            await dbContext.Entry(entity).ReloadAsync(cancellationToken);
        }

        public async Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            dbSet.Remove(entity);
        }

        public async Task RemoveAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            dbSet.RemoveRange(entities);
        }

        public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            dbContext.Entry(entity).State = EntityState.Modified;
        }

        public async Task UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            dbSet.UpdateRange(entities);
        }

        public async Task<int> ExecuteDeleteAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            IQueryable<TEntity> query = dbSet;
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            return await query.ExecuteDeleteAsync(cancellationToken);
        }

        public async Task<int> ExecuteUpdateAsync(
            Expression<Func<TEntity, bool>> predicate,
            Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            IQueryable<TEntity> query = dbSet;
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            return await query.ExecuteUpdateAsync(setPropertyCalls, cancellationToken);
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

        public async Task<TEntity> FindAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.FirstOrDefaultAsync(p => p.Id.Equals(key), cancellationToken);
        }

        public async Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var entity = await dbSet.FirstOrDefaultAsync(p => p.Id.Equals(key), cancellationToken);
            if (entity != null)
            {
                dbSet.Remove(entity);
            }
        }
    }
}
