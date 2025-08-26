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
        private ConnectionMultiplexer _connectionMultiplexer;
        private ConcurrentDictionary<int, IDatabase> _databases;
        private readonly object _lock = new object();
        private bool _disposed;
        public RedisCacheOptions Options => _options.CurrentValue;

        public StackExchangeRedis(IOptionsMonitor<RedisCacheOptions> options,
            ILogger<StackExchangeRedis> logger)
        {
            _options = options;
            Logger = logger;
            TryConnection();

            options.OnChange((_, _) =>
            {
                CreateConnection();
            });
            _ = StartHealthCheckAsync();
        }

        public bool IsConnected => _connectionMultiplexer != null && _connectionMultiplexer.IsConnected && !_disposed;

        private async Task StartHealthCheckAsync()
        {
            while (!_disposed)
            {
                TryConnection();
                await Task.Delay(TimeSpan.FromSeconds(Options.ConnectionHealthCheck));
            }
        }

        private void TryConnection()
        {
            if (IsConnected)
                return;
            lock (_lock)
            {
                if (IsConnected)
                    return;
                CreateConnection();
            }
        }

        private void CreateConnection()
        {
            lock (_lock)
            {
                try
                {
                    var configurationOptions = Options.GetConfiguredOptions();
                    _connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
                    _connectionMultiplexer.ConnectionFailed += MuxerConnectionFailed;
                    _connectionMultiplexer.ErrorMessage += MuxerErrorMessage;
                    _connectionMultiplexer.InternalError += MuxerInternalError; // 处理内部错误
                    _databases = new ConcurrentDictionary<int, IDatabase>();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to connect to Redis.");
                }
            }
        }

        public IDatabase GetDatabase(int db = -1)
        {
            if (_databases.TryGetValue(db, out var database))
                return database;

            lock (_lock)
            {
                if (_databases.TryGetValue(db, out database))
                    return database;

                var newDatabase = _connectionMultiplexer.GetDatabase(db);
                _databases.TryAdd(db, newDatabase);
                return newDatabase;
            }
        }

        #region String
        public T Get<T>(string key, int db = -1)
        {
            return RedisValueConverter.FromRedisValue<T>(GetDatabase(db).StringGet(Options.GetPrefixedKey(key)));
        }

        public async Task<T> GetAsync<T>(string key, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RedisValueConverter.FromRedisValue<T>(await GetDatabase(db).StringGetAsync(Options.GetPrefixedKey(key)));
        }

        public void Set(string key, object value, TimeSpan? expiry = null, int db = -1)
        {
            GetDatabase(db).StringSet(Options.GetPrefixedKey(key), RedisValueConverter.ToRedisValue(value), expiry);
        }

        public async Task SetAsync(string key, object value, TimeSpan? expiry = null, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await GetDatabase(db).StringSetAsync(Options.GetPrefixedKey(key), RedisValueConverter.ToRedisValue(value), expiry);
        }

        public bool SetExpireTime(string key, DateTime datetime, int db = -1)
        {
            return GetDatabase(db).KeyExpire(Options.GetPrefixedKey(key), datetime);
        }

        public async Task<bool> SetExpireTimeAsync(string key, DateTime datetime, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).KeyExpireAsync(Options.GetPrefixedKey(key), datetime);
        }

        public bool Exists(string key, int db = -1)
        {
            return GetDatabase(db).KeyExists(Options.GetPrefixedKey(key));
        }

        public async Task<bool> ExistsAsync(string key, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).KeyExistsAsync(Options.GetPrefixedKey(key));
        }

        public bool Remove(string key, int db = -1)
        {
            return GetDatabase(db).KeyDelete(Options.GetPrefixedKey(key));
        }

        public async Task RemoveAsync(string key, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await GetDatabase(db).KeyDeleteAsync(Options.GetPrefixedKey(key));
        }
        #endregion

        public long Increment(string key, int db = -1)
        {
            return GetDatabase(db).StringIncrement(Options.GetPrefixedKey(key));
        }

        public long Decrement(string key, int db = -1)
        {
            return GetDatabase(db).StringIncrement(Options.GetPrefixedKey(key));
        }

        public bool TryAcquireLock(string lockKey, string clientId, TimeSpan expiry, int retryCount = 3, int db = -1)
        {
            // 参数校验
            if (retryCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount), "Retry count must be positive");

            clientId ??= nameof(StackExchangeRedis);
            var database = GetDatabase(db);

            // 使用指数退避策略（初始100ms，最大1s）
            var baseDelayMs = 100;
            var maxDelayMs = 1000;
            var random = new Random();

            for (var attempt = 0; attempt < retryCount; attempt++)
            {
                try
                {
                    // 尝试获取锁
                    var acquired = database.StringSet(
                        Options.GetPrefixedKey(lockKey),
                        clientId,
                        expiry,
                        When.NotExists);

                    if (acquired)
                    {
                        Logger.LogInformation($"Lock acquired after {attempt} retries. Key: {lockKey}");
                        return true;
                    }

                    // 计算下一次重试的等待时间（指数退避 + 随机抖动）
                    if (attempt < retryCount - 1)
                    {
                        var exponential = Math.Pow(2, attempt);
                        var delay = (int)Math.Min(baseDelayMs * exponential, maxDelayMs);
                        delay = random.Next(delay, (int)(delay * 1.2)); // 添加20%随机抖动

                        Logger.LogDebug($"Lock contention detected. Retrying in {delay}ms...");
                        Thread.Sleep(delay);
                    }
                }
                catch (RedisException ex)
                {
                    Logger.LogWarning($"Redis error during lock acquisition (attempt {attempt + 1}): {ex.Message}");
                    if (attempt == retryCount - 1)
                        return false;
                }
            }

            Logger.LogWarning($"Failed to acquire lock after {retryCount} attempts. Key: {lockKey}");
            return false;
        }

        public void ReleaseLock(string lockKey, string clientId, int db = -1)
        {
            // 如果 clientId 为空，生成一个默认值
            clientId ??= nameof(StackExchangeRedis);

            // 使用 Lua 脚本确保释放锁的操作是原子性的
            var script = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end
    ";

            var result = GetDatabase(db).ScriptEvaluate(script, new RedisKey[] { Options.GetPrefixedKey(lockKey) }, new RedisValue[] { clientId });

            // 检查 Lua 脚本的执行结果
            if (result == null || result.IsNull)
            {
                // Lua 脚本执行失败，可能是 Redis 服务器问题
                throw new InvalidOperationException($"Failed to release lock '{lockKey}'. Redis server error.");
            }

            var resultCode = (int)result;
            if (resultCode == 0)
            {
                // 锁不存在或客户端 ID 不匹配
                var lockValue = GetDatabase(db).StringGet(Options.GetPrefixedKey(lockKey));
                if (lockValue.HasValue && lockValue.ToString() != clientId)
                {
                    // 客户端 ID 不匹配
                    throw new InvalidOperationException($"Failed to release lock '{lockKey}'. Client ID does not match.");
                }
                // 锁不存在，不抛出异常
                return;
            }

            // 锁成功释放
            Logger.LogInformation($"Lock '{lockKey}' released successfully.");
        }

        #region List
        public long ListLeftPush(string key, object value, int db = -1)
        {
            return GetDatabase(db).ListLeftPush(Options.GetPrefixedKey(key), RedisValueConverter.ToRedisValue(value));
        }

        public long ListLeftPush(string key, IEnumerable<object> value, int db = -1)
        {
            var items = new List<RedisValue>();
            foreach (var item in value)
            {
                items.Add(RedisValueConverter.ToRedisValue(item));
            }
            return GetDatabase(db).ListLeftPush(Options.GetPrefixedKey(key), items.ToArray());
        }

        public long ListRightPush(string key, object value, int db = -1)
        {
            return GetDatabase(db).ListRightPush(Options.GetPrefixedKey(key), RedisValueConverter.ToRedisValue(value));
        }

        public long ListRightPush(string key, IEnumerable<object> value, int db = -1)
        {
            var items = new List<RedisValue>();
            foreach (var item in value)
            {
                items.Add(RedisValueConverter.ToRedisValue(item));
            }
            return GetDatabase(db).ListRightPush(Options.GetPrefixedKey(key), items.ToArray());
        }

        public T ListLeftPop<T>(string key, int db = -1)
        {
            return RedisValueConverter.FromRedisValue<T>(GetDatabase(db).ListLeftPop(Options.GetPrefixedKey(key)));
        }

        public T ListRightPop<T>(string key, int db = -1)
        {
            return RedisValueConverter.FromRedisValue<T>(GetDatabase(db).ListRightPop(Options.GetPrefixedKey(key)));
        }

        public long ListLength(string key, int db = -1)
        {
            return GetDatabase(db).ListLength(Options.GetPrefixedKey(key));
        }

        public IEnumerable<T> ListRange<T>(string key, int db = -1)
        {
            var result = GetDatabase(db).ListRange(Options.GetPrefixedKey(key));
            return result.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public IEnumerable<T> ListRange<T>(string key, int start, int stop, int db = -1)
        {
            var result = GetDatabase(db).ListRange(Options.GetPrefixedKey(key), start, stop);
            return result.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public long ListRemove(string key, object value, long count = 0, int db = -1)
        {
            return GetDatabase(db).ListRemove(Options.GetPrefixedKey(key), RedisValueConverter.ToRedisValue(value), count);
        }

        public void ListClear(string key, int db = -1)
        {
            GetDatabase(db).ListTrim(Options.GetPrefixedKey(key), 1, 0);
        }
        #endregion

        #region Hash
        public bool HashSet(string key, string field, object value, int db = -1)
        {
            return GetDatabase(db).HashSet(Options.GetPrefixedKey(key), field, RedisValueConverter.ToRedisValue(value));
        }

        public T HashGet<T>(string key, string field, int db = -1)
        {
            return RedisValueConverter.FromRedisValue<T>(GetDatabase(db).HashGet(Options.GetPrefixedKey(key), field));
        }

        public Dictionary<string, T> HashGetAll<T>(string key, int db = -1)
        {
            var hashEntries = GetDatabase(db).HashGetAll(Options.GetPrefixedKey(key));
            return hashEntries.ToDictionary(entry => entry.Name.ToString(), entry => RedisValueConverter.FromRedisValue<T>(entry.Value));
        }

        public long HashDelete(string key, IEnumerable<string> fields, int db = -1)
        {
            var redisFields = fields.Select(f => (RedisValue)f).ToArray();
            return GetDatabase(db).HashDelete(Options.GetPrefixedKey(key), redisFields);
        }

        public bool HashExists(string key, string field, int db = -1)
        {
            return GetDatabase(db).HashExists(Options.GetPrefixedKey(key), field);
        }

        public IEnumerable<string> HashKeys(string key, int db = -1)
        {
            return GetDatabase(db).HashKeys(Options.GetPrefixedKey(key)).Select(k => k.ToString());
        }

        public IEnumerable<T> HashValues<T>(string key, int db = -1)
        {
            return GetDatabase(db).HashValues(Options.GetPrefixedKey(key)).Select(RedisValueConverter.FromRedisValue<T>);
        }

        public long HashLength(string key, int db = -1)
        {
            return GetDatabase(db).HashLength(Options.GetPrefixedKey(key));
        }
        #endregion

        #region Set
        public long SetAdd(string key, IEnumerable<object> values, int db = -1)
        {
            var redisValues = values.Select(RedisValueConverter.ToRedisValue).ToArray();
            return GetDatabase(db).SetAdd(Options.GetPrefixedKey(key), redisValues);
        }

        public IEnumerable<T> SetMembers<T>(string key, int db = -1)
        {
            var redisValues = GetDatabase(db).SetMembers(Options.GetPrefixedKey(key));
            return redisValues.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public bool SetContains(string key, object value, int db = -1)
        {
            var redisValue = RedisValueConverter.ToRedisValue(value);
            return GetDatabase(db).SetContains(Options.GetPrefixedKey(key), redisValue);
        }

        public long SetRemove(string key, IEnumerable<object> values, int db = -1)
        {
            var redisValues = values.Select(RedisValueConverter.ToRedisValue).ToArray();
            return GetDatabase(db).SetRemove(Options.GetPrefixedKey(key), redisValues);
        }

        public long SetLength(string key, int db = -1)
        {
            return GetDatabase(db).SetLength(Options.GetPrefixedKey(key));
        }
        #endregion

        #region Sorted Set
        public long SortedSetAdd(string key, IEnumerable<KeyValuePair<object, double>> values, int db = -1)
        {
            var sortedSetEntries = values.Select(v => new SortedSetEntry(RedisValueConverter.ToRedisValue(v.Key), v.Value)).ToArray();
            return GetDatabase(db).SortedSetAdd(Options.GetPrefixedKey(key), sortedSetEntries);
        }

        public IEnumerable<T> SortedSetRangeByRank<T>(string key, long start, long stop, int db = -1)
        {
            var redisValues = GetDatabase(db).SortedSetRangeByRank(Options.GetPrefixedKey(key), start, stop);
            return redisValues.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public IEnumerable<T> SortedSetRangeByScore<T>(string key, double min, double max, int db = -1)
        {
            var redisValues = GetDatabase(db).SortedSetRangeByScore(Options.GetPrefixedKey(key), min, max);
            return redisValues.Select(RedisValueConverter.FromRedisValue<T>);
        }

        public double? SortedSetScore(string key, object member, int db = -1)
        {
            var redisValue = RedisValueConverter.ToRedisValue(member);
            return GetDatabase(db).SortedSetScore(Options.GetPrefixedKey(key), redisValue);
        }

        public long SortedSetRemove(string key, IEnumerable<object> members, int db = -1)
        {
            var redisMembers = members.Select(RedisValueConverter.ToRedisValue).ToArray();
            return GetDatabase(db).SortedSetRemove(Options.GetPrefixedKey(key), redisMembers);
        }

        public long SortedSetLength(string key, int db = -1)
        {
            return GetDatabase(db).SortedSetLength(Options.GetPrefixedKey(key));
        }
        #endregion

        #region Stream
        public string StreamAdd(string streamKey, IDictionary<string, string> fields, string messageId = null, int db = -1)
        {
            var redisFields = fields.Select(f => new NameValueEntry(f.Key, f.Value)).ToArray();
            return GetDatabase(db).StreamAdd(Options.GetPrefixedKey(streamKey), redisFields, messageId);
        }

        public IEnumerable<(string MessageId, IDictionary<string, string> Fields)> StreamRead(string streamKey, string messageId = "0-0", int count = 10, int db = -1)
        {
            var messages = GetDatabase(db).StreamRead(Options.GetPrefixedKey(streamKey), messageId, count);
            return messages.Select(m => (
                    MessageId: m.Id.ToString(),
                    Fields: (IDictionary<string, string>)m.Values.ToDictionary(v => v.Name.ToString(), v => v.Value.ToString())
            ));
        }

        public long StreamDelete(string streamKey, IEnumerable<string> messageIds, int db = -1)
        {
            var redisMessageIds = messageIds.Select(id => (RedisValue)id).ToArray();
            return GetDatabase(db).StreamDelete(Options.GetPrefixedKey(streamKey), redisMessageIds);
        }

        public long StreamLength(string streamKey, int db = -1)
        {
            return GetDatabase(db).StreamLength(Options.GetPrefixedKey(streamKey));
        }

        public bool StreamCreateConsumerGroup(string streamKey, string groupName, string startMessageId = "0-0", int db = -1)
        {
            return GetDatabase(db).StreamCreateConsumerGroup(Options.GetPrefixedKey(streamKey), groupName, startMessageId);
        }

        public IEnumerable<(string MessageId, IDictionary<string, string> Fields)> StreamReadGroup(string streamKey, string groupName, string consumerName, int count = 10, int db = -1)
        {
            var messages = GetDatabase(db).StreamReadGroup(Options.GetPrefixedKey(streamKey), groupName, consumerName, ">", count);
            return messages.Select(m => (
                MessageId: m.Id.ToString(),
                Fields: (IDictionary<string, string>)m.Values.ToDictionary(v => v.Name.ToString(), v => v.Value.ToString())
            ));
        }

        public long StreamAcknowledge(string streamKey, string groupName, IEnumerable<string> messageIds, int db = -1)
        {
            var redisMessageIds = messageIds.Select(id => (RedisValue)id).ToArray();
            return GetDatabase(db).StreamAcknowledge(Options.GetPrefixedKey(streamKey), groupName, redisMessageIds);
        }
        #endregion

        /// <summary>
        /// redis服务器发送错误时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MuxerErrorMessage(object sender, RedisErrorEventArgs e)
        {
            if (_disposed) return;
            Logger.LogWarning("Redis physical connection fails,try connection");
            TryConnection();
        }

        /// <summary>
        /// 连接失败，如果连接成功你将不会收到这个通知
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MuxerConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            if (_disposed) return;
            Logger.LogWarning("Redis physical connection fails,try connection");
            TryConnection();
        }

        private void MuxerInternalError(object sender, InternalErrorEventArgs e)
        {
            if (_disposed) return;
            Logger.LogError(e.Exception, "Redis internal error: {Endpoint}", e.EndPoint);
            TryConnection();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _connectionMultiplexer?.Dispose();
                _databases?.Clear();
            }
            _disposed = true;
        }

        ~StackExchangeRedis()
        {
            Dispose(false);
        }

    }
}
