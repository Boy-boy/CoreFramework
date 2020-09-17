using Core.Modularity.Abstraction;
using System;
using System.Collections.Generic;

namespace Core.Modularity
{
    public class CoreModuleDescriptor : ICoreModuleDescriptor
    {
        public Type ModuleType { get; }

        public ICoreModule Instance { get; }

        public List<Type> DependedTypes { get; }

        public CoreModuleDescriptor(Type moduleType, ICoreModule instance, List<Type> dependedTypes)
        {
            ModuleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            DependedTypes = dependedTypes ?? new List<Type>();
        }
    }
}
