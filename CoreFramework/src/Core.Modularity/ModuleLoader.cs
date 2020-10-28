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
            var allCoreModuleDescriptors = CreateCoreModuleDescriptors(services, startupModuleType);

            var startupDependedModule = allCoreModuleDescriptors.FirstOrDefault(m => m.ModuleType == startupModuleType);
            return SortByCoreModuleDescriptor(startupDependedModule).Cast<ICoreModuleDescriptor>().ToArray();
        }

        private List<CoreModuleDescriptor> CreateCoreModuleDescriptors(IServiceCollection services, Type startupModuleType)
        {
            var coreModuleDescriptors = new List<CoreModuleDescriptor>();
            var allModules = CoreModuleHelper.FindAllModuleTypes(startupModuleType);
            var serviceProvider = services.BuildServiceProvider();
            foreach (var moduleType in allModules)
            {
                var instance = (ICoreModule)ActivatorUtilities.CreateInstance(serviceProvider, moduleType);
                coreModuleDescriptors.Add(new CoreModuleDescriptor(moduleType, instance));
            }
            foreach (var coreModuleDescriptor in coreModuleDescriptors)
            {
                coreModuleDescriptor.SetDependencies(coreModuleDescriptors);
            }
            return coreModuleDescriptors;
        }

        private List<ICoreModuleDescriptor> SortByCoreModuleDescriptor(CoreModuleDescriptor coreModuleDescriptor)
        {
            var sorted = new List<ICoreModuleDescriptor>();
            var visited = new Dictionary<ICoreModuleDescriptor, bool>();
            SortByDependenciesVisit(coreModuleDescriptor, m => m.Dependencies, sorted, visited);
            return sorted;
        }

        private void SortByDependenciesVisit(ICoreModuleDescriptor item,
            Func<ICoreModuleDescriptor, IEnumerable<ICoreModuleDescriptor>> getDependencies,
            List<ICoreModuleDescriptor> sorted,
            Dictionary<ICoreModuleDescriptor, bool> visited)
        {
            var alreadyVisited = visited.TryGetValue(item, out var inProcess);
            if (alreadyVisited)
            {
                if (inProcess)
                {
                    throw new ArgumentException("Cyclic dependency found! Item: " + item);
                }
            }
            else
            {
                visited[item] = true;

                var dependencies = getDependencies(item);
                if (dependencies != null)
                {
                    foreach (var dependency in dependencies)
                    {
                        SortByDependenciesVisit(dependency, getDependencies, sorted, visited);
                    }
                }
                visited[item] = false;
                sorted.Add(item);
            }
        }
    }
}
