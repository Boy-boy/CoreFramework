using System.Reflection;
using System.Runtime.Loader;
using Core.Application;
using FluentValidation;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(IRequestPreProcessor<>), typeof(ValidationRequestPreProcessor<>));

            services.AddMediatR(GetAllAssemblies().ToArray());
            services.AddValidator();
            return services;
        }

        private static void AddValidator(this IServiceCollection services)
        {
            var types = GetAllAssemblies().SelectMany(a => a.DefinedTypes)
                .Where(t => typeof(IValidator).GetTypeInfo().IsAssignableFrom(t))
                .ToList();
            foreach (var type in types)
            {
                var baseTypes = type
                    .GetInterfaces()
                    .Where(t =>
                        t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IValidator<>))
                    .Distinct();

                foreach (var baseType in baseTypes)
                {
                    services.AddTransient(baseType, type);
                }
            }
        }

        /// <summary>
        /// 获取项目程序集，排除所有的系统程序集(Microsoft.***、System.***等)、Nuget下载包
        /// </summary>
        /// <returns></returns>
        private static IList<Assembly> GetAllAssemblies()
        {
            var list = new List<Assembly>();
            var deps = DependencyContext.Default;
            //排除所有的系统程序集、Nuget下载包
            var libs = deps.CompileLibraries.Where(lib => !lib.Serviceable && lib.Type != "package");
            foreach (var lib in libs)
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(lib.Name));
                list.Add(assembly);
            }
            return list;
        }
    }
}
