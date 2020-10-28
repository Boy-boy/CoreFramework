using Core.Modularity.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Modularity
{
    public class CoreModuleDescriptor : ICoreModuleDescriptor
    {
        public Type ModuleType { get; }

        public ICoreModule Instance { get; }

        public IReadOnlyList<ICoreModuleDescriptor> Dependencies => _dependencies;

        private readonly List<ICoreModuleDescriptor> _dependencies;

        public CoreModuleDescriptor(Type moduleType, ICoreModule instance)
        {
            ModuleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _dependencies = new List<ICoreModuleDescriptor>();
        }
        public void SetDependencies(List<CoreModuleDescriptor> modules)
        {
            foreach (var dependedModuleType in CoreModuleHelper.FindDependedModuleTypes(ModuleType))
            {
                var dependedModule = modules.FirstOrDefault(m => m.ModuleType == dependedModuleType);
                if (dependedModule == null)
                {
                    throw new Exception("Could not find a depended module " + dependedModuleType.AssemblyQualifiedName + " for " + ModuleType.AssemblyQualifiedName);
                }
                _dependencies.Add(dependedModule);
            }
        }
    }
}
