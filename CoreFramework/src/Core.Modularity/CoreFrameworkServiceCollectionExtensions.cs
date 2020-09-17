using System;
using Core.Modularity.Abstraction;
using Microsoft.Extensions.DependencyInjection;

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
    }
}
