using Core.Ddd.Domain.Entities;
using Core.Ddd.Domain.Repositories;
using Core.EntityFrameworkCore.Repositories;
using Core.EntityFrameworkCore.UnitOfWork;
using Core.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EfCoreServiceCollectionExtensions
    {

        public static IServiceCollection AddDbContextAndEfRepositories<TDbContext>(this IServiceCollection services,
            Action<DbContextOptionsBuilder> optionsAction = null)
            where TDbContext : DbContext
        {
            services.AddDbContext<TDbContext>(optionsAction)
                .AddEfRepositories<TDbContext>()
                .AddUnitOfWork();
            return services;
        }

        public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(IUnitOfWork), typeof(EfCoreUnitOfWork));
            return services;
        }

        public static IServiceCollection AddEfRepositories<TDbContext>(this IServiceCollection services)
          where TDbContext : DbContext
        {
            return services.AddEfRepositories(typeof(TDbContext));
        }

        public static IServiceCollection AddEfRepositories(this IServiceCollection services, Type dbContextType)
        {
            if (!typeof(DbContext).IsAssignableFrom(dbContextType))
                throw new ArgumentException($"parameter type error,the type must inherit from [{nameof(DbContext)}]");

            var entityTypes = GetEntityTypes(dbContextType);
            foreach (var entityType in entityTypes)
            {
                services.AddEfRepository(entityType, dbContextType);
            }
            return services;
        }

        public static IServiceCollection AddEfRepository<TEntity, TDbContext>(this IServiceCollection services)
            where TEntity : class, IEntity
            where TDbContext : DbContext
        {
            return services.AddEfRepository(typeof(TEntity), typeof(TDbContext));
        }

        public static IServiceCollection AddEfRepository(this IServiceCollection services, Type entityType, Type dbContextType)
        {
            if (!typeof(IEntity).IsAssignableFrom(entityType))
                throw new ArgumentException($"parameter type error,the type must inherit from [{nameof(IEntity)}]");

            if (!typeof(DbContext).IsAssignableFrom(dbContextType))
                throw new ArgumentException($"parameter type error,the type must inherit from [{nameof(DbContext)}]");

            if (entityType.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEntity<>)))
            {
                var iKey = entityType
                    .GetInterfaces()
                    .First(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEntity<>))
                    .GenericTypeArguments
                    .Single();
                var repositoryType = typeof(IRepository<,>).MakeGenericType(entityType, iKey);
                var efCoreRepositoryType = typeof(EfCoreRepository<,,>).MakeGenericType(dbContextType, entityType, iKey);
                services.TryAddTransient(repositoryType, efCoreRepositoryType);
            }

            var repositoryType1 = typeof(IRepository<>).MakeGenericType(entityType);
            var efCoreRepositoryType1 = typeof(EfCoreRepository<,>).MakeGenericType(dbContextType, entityType);
            services.TryAddTransient(repositoryType1, efCoreRepositoryType1);
            return services;

        }

        private static IEnumerable<Type> GetEntityTypes(Type dbContextType)
        {
            return
                from property in dbContextType.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                where
                    IsAssignableToGenericType(property.PropertyType, typeof(DbSet<>)) &&
                    typeof(IEntity).IsAssignableFrom(property.PropertyType.GenericTypeArguments[0])
                select property.PropertyType.GenericTypeArguments[0];
        }

        private static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var givenTypeInfo = givenType.GetTypeInfo();

            if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            {
                return true;
            }

            foreach (var interfaceType in givenTypeInfo.GetInterfaces())
            {
                if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
            }

            if (givenTypeInfo.BaseType == null)
            {
                return false;
            }

            return IsAssignableToGenericType(givenTypeInfo.BaseType, genericType);
        }
    }
}
