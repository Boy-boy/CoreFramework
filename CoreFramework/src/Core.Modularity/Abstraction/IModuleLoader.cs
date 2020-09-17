using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.Modularity.Abstraction
{
    public interface IModuleLoader
    {
        ICoreModuleDescriptor[] LoadModules(
            IServiceCollection services,
            Type startupModuleType);
    }
}
