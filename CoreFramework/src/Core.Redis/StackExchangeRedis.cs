using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace Core.Redis
{
    public class StackExchangeRedis : IRedisCache
    {
        private readonly IOptionsMonitor<RedisCacheOptions> _options;
        public readonly ILogger<StackExchangeRedis> Logger;

        private IConnectionMultiplexer _connectionMultiplexer;
        private IConnectionMultiplexer _sentinelMultiplexer;
        private ConcurrentDictionary<int, IDatabase> _databases = new();

        private readonly Lock _lock = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly IDisposable _optionsChangeRegistration;
        private readonly Task _healthCheckTask;
        private volatile bool _disposed;

        // 健康检查兜底用的单调时钟戳（Environment.TickCount64，毫秒）。
        // _disconnectedSinceTick: 首次观察到"有连接对象但断开"的时刻；0 表示当前视为已连接。仅健康检查线程读写。
        // _lastReconnectTick: 上一次成功 (重)建连接的时刻；用于限流强制重建。多线程读写，走 Volatile。
        private long _disconnectedSinceTick;
        private long _lastReconnectTick;

        public RedisCacheOptions Options => _options.CurrentValue;

        public StackExchangeRedis(IOptionsMonitor<RedisCacheOptions> options,
            ILogger<StackExchangeRedis> logger)
        {
            _options = options;
            Logger = logger;

            // 首次连接失败不阻断 DI 启动；健康检查会持续重试
            TryConnection();

            _optionsChangeRegistration = options.OnChange((_, _) =>
            {
                if (_disposed) return;
                Logger.LogInformation("Redis configuration changes detected, re-creating connection...");
                CreateConnection();
            });

            _healthCheckTask = Task.Run(StartHealthCheckAsync);
        }

        public bool IsConnected
        {
            get
            {
                if (_disposed) return false;
                var muxer = _connectionMultiplexer;
                return muxer != null && muxer.IsConnected;
            }
        }

        private async Task StartHealthCheckAsync()
        {
            var token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    RunHealthCheck();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Redis health check failed.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, Options.ConnectionHealthCheck)), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 健康检查的核心决策：把连接生命周期尽量交还给 StackExchange.Redis 自愈，
        /// 只在两种情况下才动连接：
        ///   1) 从未建立连接对象（_connectionMultiplexer == null）：驱动无对象可自愈，必须重试；
        ///   2) 有对象但持续断开超过阈值且距上次重建已过最小间隔：兜底强制重建一次（限流，防抖动误杀）。
        /// 其余"短暂断开"一律只记录日志，等驱动内部心跳自动重连，避免斩断在途命令。
        /// </summary>
        private void RunHealthCheck()
        {
            if (_disposed) return;

            var muxer = _connectionMultiplexer;

            // 情况 1：从未连上，没有可自愈的对象，定时重试
            if (muxer == null)
            {
                TryConnection();
                return;
            }

            // 连接正常：清除断开计时
            if (muxer.IsConnected)
            {
                Volatile.Write(ref _disconnectedSinceTick, 0);
                return;
            }

            // 情况 2：有对象但断开。优先交给驱动自愈，仅在持续断开超阈值时兜底强制重建。
            var now = Environment.TickCount64;
            var disconnectedSince = Volatile.Read(ref _disconnectedSinceTick);
            if (disconnectedSince == 0)
            {
                disconnectedSince = now;
                Volatile.Write(ref _disconnectedSinceTick, now);
            }

            // 单调时钟在某些虚拟化/热迁移场景下可能出现微小回拨，Math.Max 兜底防止负数让判断失效
            var disconnectedForMs = Math.Max(0, now - disconnectedSince);

            var sustainedThresholdSec = Options.SustainedDisconnectBeforeReconnect;
            // 阈值 <= 0 表示禁用兜底强制重建，完全信任驱动自愈
            if (sustainedThresholdSec <= 0)
            {
                Logger.LogWarning(
                    "Redis disconnected for {Seconds}s, waiting for driver to self-heal (forced reconnect disabled).",
                    disconnectedForMs / 1000);
                return;
            }

            var minIntervalMs = Math.Max(0, Options.MinReconnectInterval) * 1000L;
            var sinceLastReconnectMs = Math.Max(0, now - Volatile.Read(ref _lastReconnectTick));

            if (disconnectedForMs < sustainedThresholdSec * 1000L)
            {
                Logger.LogWarning(
                    "Redis disconnected for {Seconds}s, waiting for driver to self-heal...",
                    disconnectedForMs / 1000);
                return;
            }

            if (sinceLastReconnectMs < minIntervalMs)
            {
                Logger.LogWarning(
                    "Redis still disconnected ({Seconds}s) but last reconnect was {Ago}s ago; throttled, waiting for min interval {Min}s.",
                    disconnectedForMs / 1000, sinceLastReconnectMs / 1000, minIntervalMs / 1000);
                return;
            }

            Logger.LogWarning(
                "Redis disconnected for {Seconds}s and driver did not self-heal; forcing a rate-limited reconnect.",
                disconnectedForMs / 1000);
            // 注意：不在此处重置 _disconnectedSinceTick。重建后若连上，下一拍的 IsConnected 分支会清零；
            // 若仍连不上，则继续累计断开时长，由 min-interval 控制下一次强制重建节奏。
            ForceReconnect();
        }

        /// <summary>
        /// 带锁内复检的兜底强制重建：仅供健康检查在"持续断开超阈值"时调用。
        /// 与配置热更新触发的 CreateConnection 竞争同一把 _lock；拿到锁后必须重新确认决策仍然成立，
        /// 否则会把别的线程刚重建好的连接又无谓重置一遍。
        /// </summary>
        private void ForceReconnect()
        {
            lock (_lock)
            {
                if (_disposed) return;

                // 复检 1：等锁期间，配置热更新或驱动自愈可能已经把连接修好
                var muxer = _connectionMultiplexer;
                if (muxer != null && muxer.IsConnected)
                {
                    Logger.LogInformation("Skip forced reconnect: connection already healthy (recovered while waiting for lock).");
                    return;
                }

                // 复检 2：等锁期间，可能有别的线程（如配置热更新）刚重建过连接，需重新尊重最小间隔
                var minIntervalMs = Math.Max(0, Options.MinReconnectInterval) * 1000L;
                var sinceLastReconnectMs = Math.Max(0, Environment.TickCount64 - Volatile.Read(ref _lastReconnectTick));
                if (sinceLastReconnectMs < minIntervalMs)
                {
                    Logger.LogWarning(
                        "Skip forced reconnect: another reconnect happened {Ago}s ago (within min interval {Min}s).",
                        sinceLastReconnectMs / 1000, minIntervalMs / 1000);
                    return;
                }

                CreateConnection();
            }
        }

        private void TryConnection()
        {
            if (IsConnected || _disposed)
                return;
            lock (_lock)
            {
                if (IsConnected || _disposed)
                    return;
                CreateConnection();
            }
        }

        private void CreateConnection()
        {
            lock (_lock)
            {
                if (_disposed) return;

                IConnectionMultiplexer oldMuxer = null;
                IConnectionMultiplexer oldSentinel = null;
                try
                {
                    IConnectionMultiplexer newMuxer;
                    IConnectionMultiplexer newSentinel = null;

                    if (Options.UseSentinel)
                    {
                        if (string.IsNullOrEmpty(Options.SentinelServiceName))
                            throw new InvalidOperationException(
                                "UseSentinel=true 时必须设置 RedisCacheOptions.SentinelServiceName。");

                        newSentinel = ConnectionMultiplexer.SentinelConnect(Options.GetSentinelOptions());
                        newMuxer = ((ConnectionMultiplexer)newSentinel).GetSentinelMasterConnection(Options.GetSentinelDataOptions());
                    }
                    else
                    {
                        newMuxer = ConnectionMultiplexer.Connect(Options.GetConfiguredOptions());
                    }

                    AttachMuxerEvents(newMuxer);
                    if (newSentinel != null) AttachMuxerEvents(newSentinel);

                    // 先把新连接和新字典就位，再 swap，最后释放旧连接，避免并发读 NRE
                    oldMuxer = Interlocked.Exchange(ref _connectionMultiplexer, newMuxer);
                    oldSentinel = Interlocked.Exchange(ref _sentinelMultiplexer, newSentinel);
                    Interlocked.Exchange(ref _databases, new ConcurrentDictionary<int, IDatabase>());

                    // 记录本次 (重)建时刻，供健康检查限流强制重建使用
                    Volatile.Write(ref _lastReconnectTick, Environment.TickCount64);

                    Logger.LogInformation("Redis connection has been successfully established.");
                }
                catch (Exception ex)
                {
                    // 首次或重连失败不阻断程序启动，因为底层驱动会自动进行后台重连重试
                    Logger.LogCritical(ex,
                        "Failed to connect to Redis. StackExchange.Redis will retry reconnecting automatically."
                    );
                }
                finally
                {
                    // 如果有旧连接需要释放，挪到锁外面或丢给后台线程池，避免在锁内同步等待关闭导致的性能毛刺
                    if (oldMuxer != null || oldSentinel != null)
                    {
                        Task.Run(() =>
                        {
                            DetachAndDispose(oldMuxer);
                            DetachAndDispose(oldSentinel);
                        });
                    }
                }
            }
        }

        private void AttachMuxerEvents(IConnectionMultiplexer muxer)
        {
            muxer.ConnectionFailed += MuxerConnectionFailed;
            muxer.ErrorMessage += MuxerErrorMessage;
            muxer.InternalError += MuxerInternalError;
            muxer.ConnectionRestored += MuxerConnectionRestored;
        }

        private void DetachAndDispose(IConnectionMultiplexer muxer)
        {
            if (muxer == null) return;
            try
            {
                muxer.ConnectionFailed -= MuxerConnectionFailed;
                muxer.ErrorMessage -= MuxerErrorMessage;
                muxer.InternalError -= MuxerInternalError;
                muxer.ConnectionRestored -= MuxerConnectionRestored;
            }
            catch
            {
                // 忽略事件解绑异常
            }
            try { muxer.Dispose(); } catch { /* ignore */ }
        }

        public IDatabase GetDatabase(int db = -1)
        {
            var muxer = _connectionMultiplexer;
            if (muxer == null)
                throw new InvalidOperationException("Redis 连接尚未就绪，请检查配置或等待健康检查重连。");

            // 防御性边界校验：标准 Redis 的 DB 索引范围是 0-15（默认），这里放宽到 255 防止异常刷内存
            if (db < -1 || db > 255)
                throw new ArgumentOutOfRangeException(nameof(db), "不合法的 Redis 数据库索引。");

            var databases = _databases;
            return databases.GetOrAdd(db, key => muxer.GetDatabase(key));
        }

        private RedisKey K(string key) => Options.GetPrefixedKey(key);

        #region String
        public T Get<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None)
            => RedisValueConverter.FromRedisValue<T>(GetDatabase(db).StringGet(K(key), flags));

        public async Task<T> GetAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RedisValueConverter.FromRedisValue<T>(await GetDatabase(db).StringGetAsync(K(key), flags));
        }

        public bool Set(string key, object value, TimeSpan? expiry = null, When when = When.Always, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).StringSet(K(key), RedisValueConverter.ToRedisValue(value), expiry, when, flags);

        public async Task<bool> SetAsync(string key, object value, TimeSpan? expiry = null, When when = When.Always, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).StringSetAsync(K(key), RedisValueConverter.ToRedisValue(value), expiry, when, flags);
        }

        public bool SetExpireTime(string key, DateTime datetime, int db = -1)
            => GetDatabase(db).KeyExpire(K(key), datetime);

        public async Task<bool> SetExpireTimeAsync(string key, DateTime datetime, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).KeyExpireAsync(K(key), datetime);
        }

        public bool Exists(string key, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).KeyExists(K(key), flags);

        public async Task<bool> ExistsAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).KeyExistsAsync(K(key), flags);
        }

        public bool Remove(string key, int db = -1)
            => GetDatabase(db).KeyDelete(K(key));

        public async Task<bool> RemoveAsync(string key, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).KeyDeleteAsync(K(key));
        }
        #endregion

        #region Counters
        public long Increment(string key, long value = 1, int db = -1)
            => GetDatabase(db).StringIncrement(K(key), value);

        public async Task<long> IncrementAsync(string key, long value = 1, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).StringIncrementAsync(K(key), value);
        }

        public long Decrement(string key, long value = 1, int db = -1)
            => GetDatabase(db).StringDecrement(K(key), value);

        public async Task<long> DecrementAsync(string key, long value = 1, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).StringDecrementAsync(K(key), value);
        }
        #endregion

        #region Distributed Lock
        private const int LockBaseDelayMs = 50;
        private const int LockMaxDelayMs = 500;

        // ScriptEvaluate 内部自动 EVALSHA 缓存；Prepare 后参数命名更清晰
        private static readonly LuaScript ReleaseLockScript = LuaScript.Prepare(
            "if redis.call('get', @key) == @clientId then\n" +
            "    return redis.call('del', @key)\n" +
            "else\n" +
            "    return 0\n" +
            "end");

        private static int NextLockDelayMs(int attempt)
        {
            var exponential = Math.Pow(2, attempt);
            var baseDelay = (int)Math.Min(LockBaseDelayMs * exponential, LockMaxDelayMs);
            return Random.Shared.Next(baseDelay, (int)(baseDelay * 1.2));
        }

        public async Task<bool> TryAcquireLockAsync(string lockKey, string clientId, TimeSpan expiry, int retryCount = 3, int db = -1, CancellationToken cancellationToken = default)
        {
            if (retryCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount), "Retry count must be positive");

            clientId ??= nameof(StackExchangeRedis);
            var database = GetDatabase(db);
            var prefixedKey = K(lockKey);

            for (var attempt = 0; attempt < retryCount; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (await database.StringSetAsync(prefixedKey, clientId, expiry, When.NotExists))
                    {
                        Logger.LogInformation("Lock acquired after {Attempts} retries. Key: {Key}", attempt, lockKey);
                        return true;
                    }

                    if (attempt < retryCount - 1)
                    {
                        var delay = NextLockDelayMs(attempt);
                        Logger.LogDebug("Lock contention detected. Retrying in {Delay}ms...", delay);
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                catch (RedisException ex)
                {
                    Logger.LogWarning("Redis error during lock acquisition (attempt {Attempt}): {Message}", attempt + 1, ex.Message);
                    if (attempt == retryCount - 1) return false;
                }
            }

            Logger.LogWarning("Failed to acquire lock after {Retries} attempts. Key: {Key}", retryCount, lockKey);
            return false;
        }

        public void ReleaseLock(string lockKey, string clientId, int db = -1)
        {
            clientId ??= nameof(StackExchangeRedis);
            var prefixedKey = K(lockKey);
            var result = GetDatabase(db).ScriptEvaluate(
                ReleaseLockScript,
                new { key = prefixedKey, clientId = (RedisValue)clientId });
            HandleReleaseLockResult(result, lockKey);
        }

        public async Task ReleaseLockAsync(string lockKey, string clientId, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            clientId ??= nameof(StackExchangeRedis);
            var prefixedKey = K(lockKey);
            var result = await GetDatabase(db).ScriptEvaluateAsync(
                ReleaseLockScript,
                new { key = prefixedKey, clientId = (RedisValue)clientId });
            HandleReleaseLockResult(result, lockKey);
        }

        /// <summary>
        /// 脚本返回 1 表示由当前 clientId 释放成功；返回 0 表示锁不存在或不属于当前 clientId（已过期或被他人持有），
        /// 在分布式锁语义中是正常现象，仅记录日志不抛出异常。
        /// </summary>
        private void HandleReleaseLockResult(RedisResult result, string lockKey)
        {
            if (result == null || result.IsNull)
            {
                Logger.LogWarning("Release lock '{Key}' returned null result (server error or script aborted).", lockKey);
                return;
            }

            var code = (long)result;
            if (code == 1)
                Logger.LogInformation("Lock '{Key}' released successfully.", lockKey);
            else
                Logger.LogDebug("Lock '{Key}' was not held by current clientId or already expired.", lockKey);
        }
        #endregion

        #region List
        private static RedisValue[] ToRedisValues(IEnumerable<object> values)
            => values.Select(RedisValueConverter.ToRedisValue).ToArray();

        public long ListLeftPush(string key, object value, int db = -1)
            => GetDatabase(db).ListLeftPush(K(key), RedisValueConverter.ToRedisValue(value));

        public long ListLeftPush(string key, IEnumerable<object> value, int db = -1)
            => GetDatabase(db).ListLeftPush(K(key), ToRedisValues(value));

        public async Task<long> ListLeftPushAsync(string key, object value, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).ListLeftPushAsync(K(key), RedisValueConverter.ToRedisValue(value));
        }

        public async Task<long> ListLeftPushAsync(string key, IEnumerable<object> value, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).ListLeftPushAsync(K(key), ToRedisValues(value));
        }

        public long ListRightPush(string key, object value, int db = -1)
            => GetDatabase(db).ListRightPush(K(key), RedisValueConverter.ToRedisValue(value));

        public long ListRightPush(string key, IEnumerable<object> value, int db = -1)
            => GetDatabase(db).ListRightPush(K(key), ToRedisValues(value));

        public async Task<long> ListRightPushAsync(string key, object value, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).ListRightPushAsync(K(key), RedisValueConverter.ToRedisValue(value));
        }

        public async Task<long> ListRightPushAsync(string key, IEnumerable<object> value, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).ListRightPushAsync(K(key), ToRedisValues(value));
        }

        public T ListLeftPop<T>(string key, int db = -1)
            => RedisValueConverter.FromRedisValue<T>(GetDatabase(db).ListLeftPop(K(key)));

        public async Task<T> ListLeftPopAsync<T>(string key, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RedisValueConverter.FromRedisValue<T>(await GetDatabase(db).ListLeftPopAsync(K(key)));
        }

        public T ListRightPop<T>(string key, int db = -1)
            => RedisValueConverter.FromRedisValue<T>(GetDatabase(db).ListRightPop(K(key)));

        public async Task<T> ListRightPopAsync<T>(string key, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RedisValueConverter.FromRedisValue<T>(await GetDatabase(db).ListRightPopAsync(K(key)));
        }

        public long ListLength(string key, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).ListLength(K(key), flags);

        public async Task<long> ListLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).ListLengthAsync(K(key), flags);
        }

        public IEnumerable<T> ListRange<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).ListRange(K(key), 0, -1, flags).Select(RedisValueConverter.FromRedisValue<T>);

        public IEnumerable<T> ListRange<T>(string key, int start, int stop, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).ListRange(K(key), start, stop, flags).Select(RedisValueConverter.FromRedisValue<T>);

        public async Task<IEnumerable<T>> ListRangeAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = await GetDatabase(db).ListRangeAsync(K(key), 0, -1, flags);
            return values.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public async Task<IEnumerable<T>> ListRangeAsync<T>(string key, int start, int stop, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = await GetDatabase(db).ListRangeAsync(K(key), start, stop, flags);
            return values.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public long ListRemove(string key, object value, long count = 0, int db = -1)
            => GetDatabase(db).ListRemove(K(key), RedisValueConverter.ToRedisValue(value), count);

        public async Task<long> ListRemoveAsync(string key, object value, long count = 0, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).ListRemoveAsync(K(key), RedisValueConverter.ToRedisValue(value), count);
        }

        public void ListClear(string key, int db = -1)
            => GetDatabase(db).ListTrim(K(key), 1, 0);

        public async Task ListClearAsync(string key, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await GetDatabase(db).ListTrimAsync(K(key), 1, 0);
        }
        #endregion

        #region Hash
        public bool HashSet(string key, string field, object value, int db = -1)
            => GetDatabase(db).HashSet(K(key), field, RedisValueConverter.ToRedisValue(value));

        public async Task<bool> HashSetAsync(string key, string field, object value, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).HashSetAsync(K(key), field, RedisValueConverter.ToRedisValue(value));
        }

        public T HashGet<T>(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None)
            => RedisValueConverter.FromRedisValue<T>(GetDatabase(db).HashGet(K(key), field, flags));

        public async Task<T> HashGetAsync<T>(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RedisValueConverter.FromRedisValue<T>(await GetDatabase(db).HashGetAsync(K(key), field, flags));
        }

        public Dictionary<string, T> HashGetAll<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None)
        {
            var entries = GetDatabase(db).HashGetAll(K(key), flags);
            return entries.ToDictionary(e => e.Name.ToString(), e => RedisValueConverter.FromRedisValue<T>(e.Value));
        }

        public async Task<Dictionary<string, T>> HashGetAllAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entries = await GetDatabase(db).HashGetAllAsync(K(key), flags);
            return entries.ToDictionary(e => e.Name.ToString(), e => RedisValueConverter.FromRedisValue<T>(e.Value));
        }

        private static RedisValue[] ToRedisFields(IEnumerable<string> fields)
            => fields.Select(f => (RedisValue)f).ToArray();

        public long HashDelete(string key, IEnumerable<string> fields, int db = -1)
            => GetDatabase(db).HashDelete(K(key), ToRedisFields(fields));

        public async Task<long> HashDeleteAsync(string key, IEnumerable<string> fields, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).HashDeleteAsync(K(key), ToRedisFields(fields));
        }

        public bool HashExists(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).HashExists(K(key), field, flags);

        public async Task<bool> HashExistsAsync(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).HashExistsAsync(K(key), field, flags);
        }

        public IEnumerable<string> HashKeys(string key, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).HashKeys(K(key), flags).Select(k => k.ToString());

        public async Task<IEnumerable<string>> HashKeysAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var keys = await GetDatabase(db).HashKeysAsync(K(key), flags);
            return keys.Select(k => k.ToString());
        }

        public IEnumerable<T> HashValues<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).HashValues(K(key), flags).Select(RedisValueConverter.FromRedisValue<T>);

        public async Task<IEnumerable<T>> HashValuesAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = await GetDatabase(db).HashValuesAsync(K(key), flags);
            return values.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public long HashLength(string key, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).HashLength(K(key), flags);

        public async Task<long> HashLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).HashLengthAsync(K(key), flags);
        }
        #endregion

        #region Set
        public long SetAdd(string key, IEnumerable<object> values, int db = -1)
            => GetDatabase(db).SetAdd(K(key), ToRedisValues(values));

        public async Task<long> SetAddAsync(string key, IEnumerable<object> values, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).SetAddAsync(K(key), ToRedisValues(values));
        }

        public IEnumerable<T> SetMembers<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).SetMembers(K(key), flags).Select(RedisValueConverter.FromRedisValue<T>);

        public async Task<IEnumerable<T>> SetMembersAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = await GetDatabase(db).SetMembersAsync(K(key), flags);
            return values.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public bool SetContains(string key, object value, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).SetContains(K(key), RedisValueConverter.ToRedisValue(value), flags);

        public async Task<bool> SetContainsAsync(string key, object value, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).SetContainsAsync(K(key), RedisValueConverter.ToRedisValue(value), flags);
        }

        public long SetRemove(string key, IEnumerable<object> values, int db = -1)
            => GetDatabase(db).SetRemove(K(key), ToRedisValues(values));

        public async Task<long> SetRemoveAsync(string key, IEnumerable<object> values, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).SetRemoveAsync(K(key), ToRedisValues(values));
        }

        public long SetLength(string key, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).SetLength(K(key), flags);

        public async Task<long> SetLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).SetLengthAsync(K(key), flags);
        }
        #endregion

        #region Sorted Set
        private static SortedSetEntry[] ToSortedSetEntries(IEnumerable<KeyValuePair<object, double>> values)
            => values.Select(v => new SortedSetEntry(RedisValueConverter.ToRedisValue(v.Key), v.Value)).ToArray();

        public long SortedSetAdd(string key, IEnumerable<KeyValuePair<object, double>> values, int db = -1)
            => GetDatabase(db).SortedSetAdd(K(key), ToSortedSetEntries(values));

        public async Task<long> SortedSetAddAsync(string key, IEnumerable<KeyValuePair<object, double>> values, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).SortedSetAddAsync(K(key), ToSortedSetEntries(values));
        }

        public IEnumerable<T> SortedSetRangeByRank<T>(string key, long start, long stop, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).SortedSetRangeByRank(K(key), start, stop, Order.Ascending, flags).Select(RedisValueConverter.FromRedisValue<T>);

        public async Task<IEnumerable<T>> SortedSetRangeByRankAsync<T>(string key, long start, long stop, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = await GetDatabase(db).SortedSetRangeByRankAsync(K(key), start, stop, Order.Ascending, flags);
            return values.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public IEnumerable<T> SortedSetRangeByScore<T>(string key, double min, double max, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).SortedSetRangeByScore(K(key), min, max, Exclude.None, Order.Ascending, 0, -1, flags).Select(RedisValueConverter.FromRedisValue<T>);

        public async Task<IEnumerable<T>> SortedSetRangeByScoreAsync<T>(string key, double min, double max, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = await GetDatabase(db).SortedSetRangeByScoreAsync(K(key), min, max, Exclude.None, Order.Ascending, 0, -1, flags);
            return values.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public double? SortedSetScore(string key, object member, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).SortedSetScore(K(key), RedisValueConverter.ToRedisValue(member), flags);

        public async Task<double?> SortedSetScoreAsync(string key, object member, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).SortedSetScoreAsync(K(key), RedisValueConverter.ToRedisValue(member), flags);
        }

        public long SortedSetRemove(string key, IEnumerable<object> members, int db = -1)
            => GetDatabase(db).SortedSetRemove(K(key), ToRedisValues(members));

        public async Task<long> SortedSetRemoveAsync(string key, IEnumerable<object> members, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).SortedSetRemoveAsync(K(key), ToRedisValues(members));
        }

        public long SortedSetLength(string key, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).SortedSetLength(K(key), double.NegativeInfinity, double.PositiveInfinity, Exclude.None, flags);

        public async Task<long> SortedSetLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).SortedSetLengthAsync(K(key), double.NegativeInfinity, double.PositiveInfinity, Exclude.None, flags);
        }
        #endregion

        #region Stream
        private static NameValueEntry[] ToNameValueEntries(IDictionary<string, string> fields)
            => fields.Select(f => new NameValueEntry(f.Key, f.Value)).ToArray();

        private static RedisValue[] ToRedisMessageIds(IEnumerable<string> ids)
            => ids.Select(id => (RedisValue)id).ToArray();

        private static (string MessageId, IDictionary<string, string> Fields) ToStreamTuple(StreamEntry m)
            => (m.Id.ToString(), m.Values.ToDictionary(v => v.Name.ToString(), v => v.Value.ToString()));

        public string StreamAdd(string streamKey, IDictionary<string, string> fields, string messageId = null, int db = -1)
            => GetDatabase(db).StreamAdd(K(streamKey), ToNameValueEntries(fields), messageId);

        public async Task<string> StreamAddAsync(string streamKey, IDictionary<string, string> fields, string messageId = null, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).StreamAddAsync(K(streamKey), ToNameValueEntries(fields), messageId);
        }

        public IEnumerable<(string MessageId, IDictionary<string, string> Fields)> StreamRead(string streamKey, string messageId = "0-0", int count = 10, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).StreamRead(K(streamKey), messageId, count, flags).Select(ToStreamTuple);

        public async Task<IEnumerable<(string MessageId, IDictionary<string, string> Fields)>> StreamReadAsync(string streamKey, string messageId = "0-0", int count = 10, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var messages = await GetDatabase(db).StreamReadAsync(K(streamKey), messageId, count, flags);
            return messages.Select(ToStreamTuple);
        }

        public long StreamDelete(string streamKey, IEnumerable<string> messageIds, int db = -1)
            => GetDatabase(db).StreamDelete(K(streamKey), ToRedisMessageIds(messageIds));

        public async Task<long> StreamDeleteAsync(string streamKey, IEnumerable<string> messageIds, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).StreamDeleteAsync(K(streamKey), ToRedisMessageIds(messageIds));
        }

        public long StreamLength(string streamKey, int db = -1, CommandFlags flags = CommandFlags.None)
            => GetDatabase(db).StreamLength(K(streamKey), flags);

        public async Task<long> StreamLengthAsync(string streamKey, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).StreamLengthAsync(K(streamKey), flags);
        }

        public bool StreamCreateConsumerGroup(string streamKey, string groupName, string startMessageId = "0-0", int db = -1)
            => GetDatabase(db).StreamCreateConsumerGroup(K(streamKey), groupName, startMessageId);

        public async Task<bool> StreamCreateConsumerGroupAsync(string streamKey, string groupName, string startMessageId = "0-0", int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).StreamCreateConsumerGroupAsync(K(streamKey), groupName, startMessageId);
        }

        public IEnumerable<(string MessageId, IDictionary<string, string> Fields)> StreamReadGroup(string streamKey, string groupName, string consumerName, int count = 10, int db = -1)
            => GetDatabase(db).StreamReadGroup(K(streamKey), groupName, consumerName, ">", count).Select(ToStreamTuple);

        public async Task<IEnumerable<(string MessageId, IDictionary<string, string> Fields)>> StreamReadGroupAsync(string streamKey, string groupName, string consumerName, int count = 10, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var messages = await GetDatabase(db).StreamReadGroupAsync(K(streamKey), groupName, consumerName, ">", count);
            return messages.Select(ToStreamTuple);
        }

        public long StreamAcknowledge(string streamKey, string groupName, IEnumerable<string> messageIds, int db = -1)
            => GetDatabase(db).StreamAcknowledge(K(streamKey), groupName, ToRedisMessageIds(messageIds));

        public async Task<long> StreamAcknowledgeAsync(string streamKey, string groupName, IEnumerable<string> messageIds, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).StreamAcknowledgeAsync(K(streamKey), groupName, ToRedisMessageIds(messageIds));
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Redis 服务端返回错误时。库内部会自行尝试恢复，这里仅记录日志。
        /// </summary>
        private void MuxerErrorMessage(object sender, RedisErrorEventArgs e)
        {
            if (_disposed) return;
            Logger.LogWarning("Redis error message from {Endpoint}: {Message}", e.EndPoint, e.Message);
        }

        /// <summary>
        /// 物理连接失败。StackExchange.Redis 自身会自动重连，这里只记录；
        /// 若持续不可用，由健康检查兜底执行 CreateConnection。
        /// </summary>
        private void MuxerConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            if (_disposed) return;
            Logger.LogWarning(e.Exception,
                "Redis connection failed. Endpoint={Endpoint}, ConnectionType={ConnectionType}, FailureType={FailureType}",
                e.EndPoint, e.ConnectionType, e.FailureType);
        }

        /// <summary>
        /// 物理连接恢复。
        /// </summary>
        private void MuxerConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            if (_disposed) return;
            Logger.LogInformation("Redis connection restored. Endpoint={Endpoint}", e.EndPoint);
        }

        private void MuxerInternalError(object sender, InternalErrorEventArgs e)
        {
            if (_disposed) return;
            Logger.LogError(e.Exception, "Redis internal error: {Endpoint}", e.EndPoint);
        }
        #endregion

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            try { await _cts.CancelAsync(); } catch { /* ignore */ }
            try { _optionsChangeRegistration?.Dispose(); } catch { /* ignore */ }

            if (_healthCheckTask != null)
            {
                try
                {
                    await _healthCheckTask.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch
                {
                    // 忽略后台任务退出异常（包括超时与 OperationCanceled）
                }
            }

            DetachAndDispose(Interlocked.Exchange(ref _connectionMultiplexer, null));
            DetachAndDispose(Interlocked.Exchange(ref _sentinelMultiplexer, null));

            _databases?.Clear();

            try { _cts.Dispose(); } catch { /* ignore */ }
            GC.SuppressFinalize(this);
        }
    }
}
