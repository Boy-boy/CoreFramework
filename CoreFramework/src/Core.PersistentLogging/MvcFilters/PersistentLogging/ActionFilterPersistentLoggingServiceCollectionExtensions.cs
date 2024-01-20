using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Core.PersistentLogging.MvcFilters.PersistentLogging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ActionFilterPersistentLoggingServiceCollectionExtensions
    {
        public static IServiceCollection AddActionFilterPersistentLogging(this IServiceCollection services, IConfiguration configurationRoot)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configurationRoot == null)
                throw new ArgumentNullException(nameof(configurationRoot));

            services.AddCore(configurationRoot);
            return services;
        }

        private static void AddCore(this IServiceCollection services, IConfiguration configurationRoot)
        {
            var configurationSection = configurationRoot.GetSection("ActionFilterPersistentLogging");
            services.Configure<ActionFilterPersistentLoggingOptions>(configurationSection);

            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IStartupFilter), typeof(HttpResponseBodyReadStartupFilter)));
            services.TryAddSingleton<IActionFilterPersistentLoggingStorageSourceProvider, DefaultActionFilterPersistentLoggingStorageSourceProvider>();
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IActionFilterPersistentLoggingStorageSource), typeof(LoggingActionFilterPersistentLoggingStorageSource)));
        }
    }
}
