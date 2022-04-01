using Core.Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Net;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AmazonS3StorageServiceCollectionExtensions
    {
        public static IAmazonS3Builder AddAmazonS3(this IServiceCollection services,
            Action<AmazonS3Options> configureOptions)
        {
            return services.AddAmazonS3(Options.Options.DefaultName, configureOptions);
        }

        public static IAmazonS3Builder AddAmazonS3(this IServiceCollection services,
            string name,
            Action<AmazonS3Options> configureOptions)
        {
            services.AddCore();
            services.Configure(name, configureOptions);

            return new DefaultAmazonS3Builder(services, name);
        }

        public static IAmazonS3Builder AddAmazonS3(this IServiceCollection services,
            IConfigurationSection configurationSection)
        {
            return services.AddAmazonS3(Options.Options.DefaultName, configurationSection);
        }

        public static IAmazonS3Builder AddAmazonS3(this IServiceCollection services,
            string name,
            IConfiguration configuration)
        {
            services.AddCore();
            services.Configure<AmazonS3Options>(name, configuration);

            return new DefaultAmazonS3Builder(services, name);
        }

        public static IAmazonS3Builder AddAmazonS3<TClient>(this IServiceCollection services,
            string name,
            Action<AmazonS3Options> configureOptions)
            where TClient : class
        {
            return services.AddAmazonS3<TClient, TClient>(name, configureOptions);
        }

        public static IAmazonS3Builder AddAmazonS3<TClient, TImplementation>(this IServiceCollection services,
            string name,
            Action<AmazonS3Options> configureOptions)
            where TClient : class
            where TImplementation : class, TClient
        {
            services.AddCore();
            services.AddTypedAmazonS3ClientCore<TClient, TImplementation>(name);
            services.Configure(name, configureOptions);

            return new DefaultAmazonS3Builder(services, name);
        }

        public static IAmazonS3Builder AddAmazonS3<TClient>(this IServiceCollection services,
            string name,
            IConfiguration configuration)
            where TClient : class
        {
            return services.AddAmazonS3<TClient, TClient>(name, configuration);
        }

        public static IAmazonS3Builder AddAmazonS3<TClient, TImplementation>(this IServiceCollection services,
            string name,
            IConfiguration configuration)
            where TClient : class
            where TImplementation : class, TClient
        {
            services.AddCore();
            services.AddTypedAmazonS3ClientCore<TClient, TImplementation>(name);
            services.Configure<AmazonS3Options>(name, configuration);

            return new DefaultAmazonS3Builder(services, name);
        }

        private static void AddCore(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddLogging();
            services.AddOptions();

            services.TryAddSingleton<IAmazonS3ClientFactory, DefaultAmazonS3ClientFactory>();

            services.TryAddTransient(typeof(ITypeClientFactory<>), typeof(DefaultTypeClientFactory<>));
            services.TryAdd(ServiceDescriptor.Singleton(typeof(DefaultTypeClientFactory<>.Cache), typeof(DefaultTypeClientFactory<>.Cache)));

            services.AddHttpClient(AmazonS3ClientHttpClientNameConstants.AmazonS3ClientHttpClientName)
                .ConfigurePrimaryHttpMessageHandler(provider =>
                {
                    var option = provider.GetRequiredService<IOptionsMonitor<AmazonS3Options>>().CurrentValue;
                    var handler = new HttpClientHandler
                    {
                        MaxConnectionsPerServer = option.MaxConnectionsPerServer,
                        AllowAutoRedirect = option.AllowAutoRedirect,
                        AutomaticDecompression = DecompressionMethods.None,
                    };
                    return handler;
                })
                .ConfigureHttpClient((provider, client) =>
                {
                    var option = provider.GetRequiredService<IOptionsMonitor<AmazonS3Options>>().CurrentValue;
                    if (option.Timeout.HasValue)
                        client.Timeout = option.Timeout.Value;
                });
        }

        public static void AddTypedAmazonS3ClientCore<TClient, TImplementation>(
            this IServiceCollection services,
            string name)
            where TClient : class
            where TImplementation : class, TClient
        {
            services.AddTransient<TClient>(serviceProvider =>
            {
                var amazonS3Client = serviceProvider.GetRequiredService<IAmazonS3ClientFactory>().CreateClient(name);
                var typedClientFactory = serviceProvider.GetRequiredService<ITypeClientFactory<TImplementation>>();
                return typedClientFactory.CreateClient(amazonS3Client);
            });
        }
    }
}
