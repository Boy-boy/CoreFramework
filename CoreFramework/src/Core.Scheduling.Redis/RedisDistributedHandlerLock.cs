using Core.Scheduling.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Core.Scheduling.Redis
{
    /// <summary>
    /// Redis 分布式锁实现。SET NX PX 抢锁 + Lua 校验脚本释放/续租,
    /// 用 16 字节随机 token 防止误删别人的锁。
    /// 集群部署时通过 <c>AddCoreSchedulingRedisLock</c> 替换默认 noop 实现。
    /// </summary>
    internal sealed class RedisDistributedHandlerLock : IDistributedHandlerLock
    {
        // @占位符自动映射匿名对象参数，SDK自动拆分为KEYS和ARGV
        private const string ReleaseScript =
            "if redis.call('GET', @key) == @token then return redis.call('DEL', @key) else return 0 end";

        private const string RenewScript =
            "if redis.call('GET', @key) == @token then return redis.call('PEXPIRE', @key, tonumber(@ms)) else return 0 end";

        private static readonly LuaScript PreparedReleaseScript = LuaScript.Prepare(ReleaseScript);
        private static readonly LuaScript PreparedRenewScript = LuaScript.Prepare(RenewScript);

        private readonly IConnectionMultiplexer _multiplexer;
        private readonly RedisSchedulingOptions _options;
        private readonly ILogger<RedisDistributedHandlerLock> _logger;
        private readonly string _normalizedPrefix;

        public RedisDistributedHandlerLock(
            IConnectionMultiplexer multiplexer,
            IOptions<RedisSchedulingOptions> options,
            ILogger<RedisDistributedHandlerLock> logger)
        {
            _multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
            _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 构造期规整化前缀,运行期 BuildKey 只做一次 Concat
            _normalizedPrefix = string.IsNullOrEmpty(_options.KeyPrefix)
                ? string.Empty
                : _options.KeyPrefix.TrimEnd(':') + ":";
        }

        public async Task<IDistributedHandlerLockHandle?> TryAcquireAsync(
            string handlerCode,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(handlerCode))
                throw new ArgumentException("HandlerCode must be non-empty.", nameof(handlerCode));
            if (leaseDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(leaseDuration), leaseDuration, "Lease must be positive.");

            cancellationToken.ThrowIfCancellationRequested();
            var db = _multiplexer.GetDatabase(_options.Database);
            var key = BuildKey(handlerCode);
            // 16 字节 GUID 当作 RedisValue,省去 32 字符 hex 字符串分配
            RedisValue token = Guid.NewGuid().ToByteArray();

            try
            {
                var acquired = await db.StringSetAsync(
                    key,
                    token,
                    leaseDuration,
                    When.NotExists,
                    CommandFlags.DemandMaster).ConfigureAwait(false);

                return !acquired
                    ? null
                    : new RedisLockHandle(db, key, token, handlerCode, _logger);
            }
            catch (RedisException ex)   // 只捕 Redis 自己的异常,不吞 OOM/Cancel
            {
                _logger.LogWarning(ex, "Redis acquire failed for {HandlerCode}; skipping this tick.", handlerCode);
                throw;
            }
        }

        private string BuildKey(string handlerCode)
            => _normalizedPrefix.Length == 0 ? handlerCode : _normalizedPrefix + handlerCode;

        private sealed class RedisLockHandle : IDistributedHandlerLockHandle
        {
            private readonly IDatabase _db;
            private readonly RedisKey _key;
            private readonly RedisValue _token;
            private readonly ILogger _logger;
            private int _disposed;

            public RedisLockHandle(IDatabase db, RedisKey key, RedisValue token, string handlerCode, ILogger logger)
            {
                _db = db;
                _key = key;
                _token = token;
                HandlerCode = handlerCode;
                _logger = logger;
            }

            public string HandlerCode { get; }

            public async Task<bool> RenewAsync(TimeSpan leaseDuration, CancellationToken cancellationToken)
            {
                if (leaseDuration <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(leaseDuration), leaseDuration, "Lease must be positive.");

                if (Volatile.Read(ref _disposed) != 0)
                    return false;

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var ms = leaseDuration.Ticks / TimeSpan.TicksPerMillisecond;

                    var result = await PreparedRenewScript.EvaluateAsync(
                        _db,
                        new { key = _key, token = _token, ms = ms },
                        flags: CommandFlags.DemandMaster).ConfigureAwait(false);

                    return (long?)result == 1;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Renew lock failed for {HandlerCode}", HandlerCode);
                    return false;
                }
            }

            public async ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                try
                {
                    await PreparedReleaseScript.EvaluateAsync(
                        _db,
                        new { key = _key, token = _token },
                        flags: CommandFlags.DemandMaster).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Redis lock release failed for handler {HandlerCode}; relying on TTL expiry.",
                        HandlerCode);
                }
            }
        }
    }
}