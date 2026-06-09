using Core.Alert;
using Core.Redis;
using Microsoft.Extensions.Options;

namespace Core.Alert.Redis
{
    /// <summary>
    /// 基于框架 <see cref="IRedisCache"/> 的告警状态存储实现。
    /// 该实现提供跨重启状态保留，但不额外提供多实例间的分布式原子去重。
    /// </summary>
    public sealed class RedisAlertStorageProvider : IAlertStorageProvider
    {
        private readonly IRedisCache _redisCache;
        private readonly IOptionsMonitor<AlertRedisStorageOptions> _options;

        public RedisAlertStorageProvider(
            IRedisCache redisCache,
            IOptionsMonitor<AlertRedisStorageOptions> options)
        {
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<AlertSessionState> GetAsync(string sessionKey)
        {
            var options = _options.CurrentValue;
            return _redisCache.GetJsonAsync<AlertSessionState>(BuildKey(sessionKey, options), options.Database);
        }

        public async Task SetAsync(string sessionKey, AlertSessionState state)
        {
            var options = _options.CurrentValue;
            await _redisCache.SetJsonAsync(BuildKey(sessionKey, options), state, options.StateTtl, db: options.Database);
        }

        public async Task RemoveAsync(string sessionKey)
        {
            var options = _options.CurrentValue;
            await _redisCache.RemoveAsync(BuildKey(sessionKey, options), options.Database);
        }

        private static string BuildKey(string sessionKey, AlertRedisStorageOptions options)
            => string.Concat(options.KeyPrefix ?? string.Empty, sessionKey);
    }
}
