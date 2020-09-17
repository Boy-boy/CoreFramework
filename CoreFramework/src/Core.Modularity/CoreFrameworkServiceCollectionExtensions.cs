using System;
using System.Linq;
using Core.Modularity.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Modularity
{
    public static class CoreFrameworkServiceCollectionExtensions
    {
        public static void AddApplication<T>(this IServiceCollection service)
        where T : ICoreModule
        {
            AddApplication(service, typeof(T));
        }

        public static void AddApplication(this IServiceCollection service, Type startupModuleType)
        {
            CoreApplicationFactory.CreateCoreApplication(startupModuleType, service);
        }

        public static IConfiguration GetConfiguration(this IServiceCollection services)
        {
            var configuration = ((HostBuilderContext)services
                .FirstOrDefault(p => p.ServiceType == typeof(HostBuilderContext))?.ImplementationInstance)
                ?.Configuration;
            return configuration;
        }
    }
}
