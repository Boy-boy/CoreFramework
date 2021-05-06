using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.HttpClient
{
    public static class HttpClientServiceCollectionExtensions
    {
        public static IServiceCollection AddBaseHttpClient(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            services.AddHttpClient<IBaseHttpClient, BaseHttpClient>();
            return services;
        }
        public static IServiceCollection AddBaseHttpClient(this IServiceCollection services, Action<System.Net.Http.HttpClient> options)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            services.AddHttpClient<IBaseHttpClient, BaseHttpClient>(options);
            return services;
        }
    }
}
