using Core.Modularity.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.Modularity
{
    public static class CoreApplicationManagerFactory
    {
        public static ICoreApplicationManager CreateCoreApplication(
            Type startupModuleType,
            IServiceCollection services)
        {
            return new CoreApplicationManager(startupModuleType, services);
        }
    }
}
