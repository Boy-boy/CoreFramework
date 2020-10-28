using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Modularity.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Modularity
{
    public class CoreApplicationManager : ICoreApplicationManager
    {
        public Type StartupModuleType { get; }

        public IReadOnlyList<ICoreModuleDescriptor> Modules { get; }

        public CoreApplicationManager(
            Type startupModuleType,
            IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            StartupModuleType = startupModuleType ?? throw new ArgumentNullException(nameof(startupModuleType));

            services.TryAddSingleton<ICoreApplicationManager>(this);
            services.TryAddSingleton<IModuleLoader>(new ModuleLoader());
            Modules = LoadModules(services);
            ConfigureConfigureServices(services);
        }


        public void ConfigureConfigureServices(IServiceCollection services)
        {
            var context = new ServiceCollectionContext(services);
            foreach (var moduleDescriptor in Modules)
            {
                try
                {

                    moduleDescriptor.Instance.PreConfigureServices(context);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred during PreConfigureServices phase of the module " + moduleDescriptor.ModuleType.AssemblyQualifiedName + ". See the inner exception for details.", ex);
                }
            }
            foreach (var moduleDescriptor in Modules)
            {
                try
                {

                    moduleDescriptor.Instance.ConfigureServices(context);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred during ConfigureServices phase of the module " + moduleDescriptor.ModuleType.AssemblyQualifiedName + ". See the inner exception for details.", ex);
                }
            }
            foreach (var moduleDescriptor in Modules)
            {
                try
                {

                    moduleDescriptor.Instance.PostConfigureServices(context);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred during PostConfigureServices phase of the module " + moduleDescriptor.ModuleType.AssemblyQualifiedName + ". See the inner exception for details.", ex);
                }
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            var context = new ApplicationBuilderContext(app);
            foreach (var moduleDescriptor in Modules)
            {
                try
                {

                    moduleDescriptor.Instance.PreConfigure(context);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred during PreConfigure phase of the module " + moduleDescriptor.ModuleType.AssemblyQualifiedName + ". See the inner exception for details.", ex);
                }
            }
            foreach (var moduleDescriptor in Modules)
            {
                try
                {

                    moduleDescriptor.Instance.Configure(context);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred during Configure phase of the module " + moduleDescriptor.ModuleType.AssemblyQualifiedName + ". See the inner exception for details.", ex);
                }
            }
            foreach (var moduleDescriptor in Modules)
            {
                try
                {

                    moduleDescriptor.Instance.PostConfigure(context);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred during PostConfigure phase of the module " + moduleDescriptor.ModuleType.AssemblyQualifiedName + ". See the inner exception for details.", ex);
                }
            }
        }

        public void Shutdown(IServiceProvider serviceProvider)
        {
            var context = new ShutdownApplicationContext(serviceProvider);
            foreach (var moduleDescriptor in Modules)
            {
                try
                {
                    moduleDescriptor.Instance.Shutdown(context);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred during Shutdown phase of the module  " + moduleDescriptor.ModuleType.AssemblyQualifiedName + ". See the inner exception for details.", ex);
                }
            }
        }

        private IReadOnlyList<ICoreModuleDescriptor> LoadModules(
            IServiceCollection services)
        {
            var moduleLoader = (IModuleLoader)services.FirstOrDefault(p => p.ServiceType == typeof(IModuleLoader))?.ImplementationInstance;
            return moduleLoader?.LoadModules(services, StartupModuleType);
        }
    }
}
