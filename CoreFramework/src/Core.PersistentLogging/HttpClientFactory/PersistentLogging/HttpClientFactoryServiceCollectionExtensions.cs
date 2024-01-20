using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Core.PersistentLogging.HttpClientFactory.PersistentLogging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientFactoryServiceCollectionExtensions
    {
        public static IHttpClientBuilder AddPersistentLoggingHttpMessageHandler(this IHttpClientBuilder builder, IConfiguration configurationRoot)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (configurationRoot == null)
                throw new ArgumentNullException(nameof(configurationRoot));

            builder.Services.AddCore(configurationRoot, builder.Name);

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options => options.HttpMessageHandlerBuilderActions.Add(b => b.AdditionalHandlers.Add(ActivatorUtilities.CreateInstance<PersistentLoggingHttpMessageHandler>(b.Services, builder.Name))));
            return builder;
        }

        private static void AddCore(this IServiceCollection services, IConfiguration configurationRoot, string name)
        {
            var configurationSection = configurationRoot.GetSection("HttpClientPersistentLogging");
            services.Configure<HttpClientPersistentLoggingOptions>(name, configurationSection);

            services.TryAddSingleton<IHttpClientPersistentLoggingStorageSourceProvider, DefaultHttpClientPersistentLoggingStorageSourceProvider>();
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IHttpClientPersistentLoggingStorageSource), typeof(LoggingActionFilterPersistentLoggingStorageSource)));
        }
    }
}
