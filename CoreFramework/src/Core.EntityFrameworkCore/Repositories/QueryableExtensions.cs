using Core.Ddd.Domain.Entities;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Core.EntityFrameworkCore.Repositories
{
    public static class QueryableExtensions
    {
        public static IQueryable<TEntity> Where<TEntity>(this IQueryable<TEntity> queryable, Expression<Func<TEntity, bool>> predicate, bool applyPredicate)
            where TEntity : IEntity
        {
            return applyPredicate ? queryable.Where(predicate) : queryable;
        }
    }
}
