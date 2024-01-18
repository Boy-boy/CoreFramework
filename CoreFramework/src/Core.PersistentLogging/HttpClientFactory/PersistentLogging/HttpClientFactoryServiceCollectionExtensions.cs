using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Core.PersistentLogging.HttpClientFactory.PersistentLogging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientFactoryServiceCollectionExtensions
    {
        public static IServiceCollection AddLoggingHttpMessageHandlerBuilderFilter(this IServiceCollection services, IConfiguration configurationRoot)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configurationRoot == null)
                throw new ArgumentNullException(nameof(configurationRoot));

            services.AddCore(configurationRoot);

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, LoggingHttpMessageHandlerBuilderFilter>());
            return services;
        }

        public static IHttpClientBuilder AddPersistentLoggingHttpMessageHandler(this IHttpClientBuilder builder, IConfiguration configurationRoot)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (configurationRoot == null)
                throw new ArgumentNullException(nameof(configurationRoot));

            builder.Services.AddCore(configurationRoot);

            builder.Services.TryAddSingleton<PersistentLoggingHttpMessageHandler>();
            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options => options.HttpMessageHandlerBuilderActions.Add(b => b.AdditionalHandlers.Add(b.Services.GetRequiredService<PersistentLoggingHttpMessageHandler>())));
            return builder;
        }

        private static void AddCore(this IServiceCollection services, IConfiguration configurationRoot)
        {
            var configurationSection = configurationRoot.GetSection("HttpClientPersistentLogging");
            services.Configure<HttpClientPersistentLoggingOptions>(configurationSection);

            services.TryAddSingleton<IHttpClientPersistentLoggingStorageSourceProvider, DefaultHttpClientPersistentLoggingStorageSourceProvider>();
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IHttpClientPersistentLoggingStorageSource), typeof(LoggingActionFilterPersistentLoggingStorageSource)));
        }
    }
}
