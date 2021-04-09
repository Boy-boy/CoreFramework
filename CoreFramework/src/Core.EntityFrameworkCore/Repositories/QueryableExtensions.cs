using Core.Ddd.Domain.Entities;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Core.EntityFrameworkCore.Repositories
{
    public static class QueryableExtensions
    {
        public static IQueryable<TEntity> IfWhere<TEntity>(this IQueryable<TEntity> queryable, Expression<Func<TEntity, bool>> expression, bool hasValue)
            where TEntity : IEntity
        {
            return hasValue ? queryable.Where(expression) : queryable;
        }
    }
}
