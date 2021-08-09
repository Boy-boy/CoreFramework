using System;
using Core.Configuration;
using Core.Configuration.Storage;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigurationServiceCollectionExtensions
    {
        public static IServiceCollection AddDbConfiguration(this IServiceCollection services, Action<ConfigurationOptions> configureOptions = null)
        {
            services.TryAddSingleton<IConfigurationStorage>(_ => ConfigurationStorageExtensions.ConfigurationStorage);

            if (configureOptions == null)
                return services;

            var options = new ConfigurationOptions();
            configureOptions.Invoke(options);
            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }

            return services;
        }

    }
}
