using Core.Alert;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisStorageCollectionExtensions
    {
        public static AlertOptions AddRedis(
            this AlertOptions options,
            Action<Core.Alert.Redis.AlertRedisStorageOptions> actionOptions)
        {
            options.AddExtensions(new Core.Alert.Redis.AlertOptionsExtensions(actionOptions));
            return options;
        }

        public static AlertOptions AddRedis(
            this AlertOptions options,
            IConfiguration configuration)
        {
            options.AddExtensions(new Core.Alert.Redis.AlertOptionsExtensions(configuration));
            return options;
        }
    }
}
