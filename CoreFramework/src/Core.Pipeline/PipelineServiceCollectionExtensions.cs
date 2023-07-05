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

            services.TryAddSingleton(typeof(IPipelineBuilderFactory), typeof(DefaultPipelineBuilderFactory));
            services.TryAddTransient(typeof(IPipelineProvider), typeof(DefaultPipelineProvider));
            services.AddTransient(typeof(IPipeline<>), typeof(RequestPrePipeline<>));
            services.AddTransient(typeof(IPipeline<>), typeof(RequestProPipeline<>));

            services.RegistrarClass(assemblies);
            return services;
        }

        private static void RegistrarClass(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies == null)
                return;

            var typeArray = new[]
            {
                typeof(IPipeline<>),
                typeof(IRequestHandler<>),
                typeof(IRequestPreHandler<>),
                typeof(IRequestProHandler<>),
            };
            foreach (var type in typeArray)
            {
                services.RegistrarClass(type, assemblies);
            }
        }

        private static void RegistrarClass(this IServiceCollection services, Type parentType, params Assembly[] assemblies)
        {
            if (assemblies == null)
                return;

            var registerTypes = GetRegisterTypes(parentType, assemblies);
            foreach (var registerType in registerTypes)
            {
                var baseRegisterTypes = GetBaseRegisterTypes(parentType, registerType);
                foreach (var baseRegisterType in baseRegisterTypes)
                {
                    services.AddTransient(baseRegisterType, registerType);
                    services.AddTransient(registerType);
                }
            }
        }

        public static IEnumerable<Type> GetRegisterTypes(Type parentType, params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
                return new List<Type>();
            return assemblies.SelectMany(a => a.DefinedTypes)
                .Where(t => t.GetInterfaces().Any(f => f.IsGenericType && f.GetGenericTypeDefinition() == parentType))
                .ToList();
        }

        public static IEnumerable<Type> GetBaseRegisterTypes(Type parentType, Type registerType)
        {
            var baseRegisterTypes = registerType
                .GetInterfaces()
                .Where(t =>
                    t.IsGenericType && t.GetGenericTypeDefinition() == parentType);
            baseRegisterTypes = baseRegisterTypes.Distinct();
            return baseRegisterTypes;
        }

    }
}
