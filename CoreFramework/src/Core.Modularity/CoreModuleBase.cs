using Core.Modularity.Abstraction;
using System;
using System.Reflection;

namespace Core.Modularity
{
    public abstract class CoreModuleBase : ICoreModule
    {
        public virtual void PreConfigureServices(ServiceCollectionContext context)
        {
        }

        public virtual void ConfigureServices(ServiceCollectionContext context)
        {
        }

        public virtual void PostConfigureServices(ServiceCollectionContext context)
        {
        }

        public virtual void PreConfigure(ConfigureApplicationContext context)
        {
        }

        public virtual void Configure(ConfigureApplicationContext context)
        {
        }

        public virtual void PostConfigure(ConfigureApplicationContext context)
        {
        }

        public virtual void Shutdown(ShutdownApplicationContext context)
        {
        }

        public static bool IsCoreModule(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return
                typeInfo.IsClass &&
                !typeInfo.IsAbstract &&
                !typeInfo.IsGenericType &&
                typeof(ICoreModule).GetTypeInfo().IsAssignableFrom(type);
        }

        public static void CheckCoreModuleType(Type moduleType)
        {
            if (!IsCoreModule(moduleType))
            {
                throw new ArgumentException("Given type is not an Core module: " + moduleType.AssemblyQualifiedName);
            }
        }
    }
}
