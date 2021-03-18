using Core.Modularity.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.Modularity
{
    public static class CoreFrameworkServiceCollectionExtensions
    {
        public static void ConfigureServiceCollection<T>(this IServiceCollection service)
        where T : ICoreModule
        {
            ConfigureServiceCollection(service, typeof(T));
        }

        public static void ConfigureServiceCollection(this IServiceCollection service, Type startupModuleType)
        {
            CoreApplicationManagerFactory.CreateCoreApplication(startupModuleType, service);
        }
    }
}
