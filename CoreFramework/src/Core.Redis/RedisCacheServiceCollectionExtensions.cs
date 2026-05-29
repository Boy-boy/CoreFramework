using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Redis
{
    public static class RedisCacheServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<RedisCacheOptions> options = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddSingleton<IRedisCache, StackExchangeRedis>();
            if (options != null)
                services.Configure(options);
            return services;
        }
    }
}
