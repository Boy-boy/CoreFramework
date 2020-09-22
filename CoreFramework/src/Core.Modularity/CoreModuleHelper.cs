using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Modularity.Attribute;

namespace Core.Modularity
{
    public class CoreModuleHelper
    {
        public static List<Type> FindAllModuleTypes(Type startupModuleType)
        {
            var moduleTypes = new List<Type>();
            AddModules(moduleTypes, startupModuleType);
            return moduleTypes;
        }
        public static List<Type> FindDependedModuleTypes(Type moduleType)
        {
            CoreModuleBase.CheckCoreModuleType(moduleType);
            var source = new List<Type>();
            foreach (var dependedTypes in moduleType.GetCustomAttributes().OfType<DependsOnAttribute>())
            {
                foreach (var dependedType in dependedTypes.GetDependedTypes())
                {
                    if (!source.Contains(dependedType))
                        source.Add(dependedType);
                }
            }
            return source;
        }

        private static void AddModules(List<Type> moduleTypes, Type moduleType)
        {
            CoreModuleBase.CheckCoreModuleType(moduleType);
            if (moduleTypes.Contains(moduleType))
                return;
            foreach (var dependedModuleType in FindDependedModuleTypes(moduleType))
                AddModules(moduleTypes, dependedModuleType);
            moduleTypes.Add(moduleType);
        }
    }
}
