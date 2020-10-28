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
        public static void AddApplicationManager<T>(this IServiceCollection service)
        where T : ICoreModule
        {
            AddApplicationManager(service, typeof(T));
        }

        public static void AddApplicationManager(this IServiceCollection service, Type startupModuleType)
        {
            CoreApplicationManagerFactory.CreateCoreApplication(startupModuleType, service);
        }
    }
}
