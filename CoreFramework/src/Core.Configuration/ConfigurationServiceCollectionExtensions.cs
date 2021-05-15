using Core.Configuration.Storage;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigurationServiceCollectionExtensions
    {
        public static IServiceCollection AddDbConfiguration(this IServiceCollection services)
        {
            services.TryAddSingleton<IConfigurationStorage>(_ => ConfigurationStorageExtensions.ConfigurationStorage);
            return services;
        }

    }
}
