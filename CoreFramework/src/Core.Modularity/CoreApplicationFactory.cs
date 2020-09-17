using Core.Modularity.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.Modularity
{
    public static class CoreApplicationFactory
    {
        public static ICoreApplication CreateCoreApplication(
            Type startupModuleType,
            IServiceCollection services)
        {
            return new CoreApplication(startupModuleType, services);
        }
    }
}
