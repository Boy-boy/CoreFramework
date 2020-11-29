using Core.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CoreEntityFrameworkServiceCollectionExtensions
    {
        public static IServiceCollection AddDbContextSharding<TContext>(
           this IServiceCollection serviceCollection,
            Action<DbContextOptionsBuilder> optionsAction = null,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
            where TContext : DbContext
        {
            return AddDbContextSharding<TContext, TContext>(serviceCollection, optionsAction, contextLifetime, optionsLifetime);
        }

        public static IServiceCollection AddDbContextSharding<TContextService, TContextImplementation>(
            this IServiceCollection serviceCollection,
            Action<DbContextOptionsBuilder> optionsAction = null,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
            where TContextImplementation : DbContext, TContextService
        {
            return serviceCollection.AddDbContext<TContextService, TContextImplementation>(options =>
            {
                var extension = (options.Options.FindExtension<CoreOptionsExtension>() ?? new CoreOptionsExtension())
                    .WithReplacedService(typeof(IModelSource), typeof(CoreModelSource));
                ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(extension);
                optionsAction?.Invoke(options);
            }, contextLifetime, optionsLifetime);
        }
    }
}
