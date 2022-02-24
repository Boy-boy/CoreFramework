using System.Reflection;
using Core.Pipeline;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PipelineServiceCollectionExtensions
    {
        public static IServiceCollection AddPipeline(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton(typeof(IPipelineProvider), typeof(DefaultPipelineProvider));
            services.TryAddSingleton(typeof(IPipelineBuilderFactory), typeof(DefaultPipelineBuilderFactory));

            services.RegistrarPipeline(assemblies);
            return services;
        }

        private static void RegistrarPipeline(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies == null)
                return;

            var handlerTypes = GetPipelineTypes(assemblies);
            foreach (var handlerType in handlerTypes)
            {
                var baseHandlerTypes = GetBasePipelineTypes(handlerType);
                foreach (var baseHandlerType in baseHandlerTypes)
                {
                    services.AddTransient(baseHandlerType, handlerType);
                    services.AddTransient(handlerType);
                }
            }
        }

        public static IEnumerable<Type> GetPipelineTypes(params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
                return new List<Type>();
            return assemblies.SelectMany(a => a.DefinedTypes)
                .Where(t => typeof(IPipeline).GetTypeInfo().IsAssignableFrom(t))
                .ToList();
        }

        public static IEnumerable<Type> GetBasePipelineTypes(Type handlerType)
        {
            var baseHandlerTypes = handlerType
                .GetInterfaces()
                .Where(t =>
                    t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IPipeline<>));
            baseHandlerTypes = baseHandlerTypes.Distinct();
            return baseHandlerTypes;
        }
    }
}
