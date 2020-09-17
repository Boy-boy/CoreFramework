using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Modularity.Abstraction;

namespace Core.Modularity
{
    public class ModuleLoader : IModuleLoader
    {
        public ICoreModuleDescriptor[] LoadModules(IServiceCollection services, Type startupModuleType)
        {
            var allCoreModuleDescriptors = new List<CoreModuleDescriptor>();
            FillModules(allCoreModuleDescriptors, services, startupModuleType);
            return allCoreModuleDescriptors.Cast<ICoreModuleDescriptor>().ToArray();
        }

        private void FillModules(
            List<CoreModuleDescriptor> modules,
            IServiceCollection services,
            Type startupModuleType)
        {
            var allModules = CoreModuleHelper.FindAllModuleTypes(startupModuleType);
            var serviceProvider = services.BuildServiceProvider();
            foreach (var moduleType in allModules)
            {
                var dependedTypes = CoreModuleHelper.FindDependedModuleTypes(moduleType);
                var instance = (ICoreModule)ActivatorUtilities.CreateInstance(serviceProvider, moduleType);
                modules.Add(new CoreModuleDescriptor(moduleType, instance, dependedTypes));
            }

        }
    }
}
