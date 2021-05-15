using Core.Json.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Redis
{
    public class StackExchangeRedis : IRedisCache
    {
        private readonly IOptionsMonitor<RedisCacheOptions> _options;
        private readonly ILogger<StackExchangeRedis> _logger;
        private ConnectionMultiplexer _connectionMultiplexer;
        private ConcurrentDictionary<int, IDatabase> _databases;
        private readonly object _lock = new object();
        private static readonly TimerCallback TimerCallback = s => ((StackExchangeRedis)s)?.TryConnection();
        private Timer _timer;
        private bool _disposed;

        public StackExchangeRedis(IOptionsMonitor<RedisCacheOptions> options,
            ILogger<StackExchangeRedis> logger)
        {
            _options = options;
            _logger = logger;
            TryConnection();
            _timer = new Timer(TimerCallback, this, TimeSpan.FromSeconds(options.CurrentValue.ConnectionHealthCheck), TimeSpan.FromSeconds(options.CurrentValue.ConnectionHealthCheck));

            options.OnChange((option, str) =>
            {
                CreateConnection();
            });
        }

        public bool IsConnected => _connectionMultiplexer != null && _connectionMultiplexer.IsConnected && !_disposed;

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
                _connectionMultiplexer = ConnectionMultiplexer.Connect(_options.CurrentValue.Configuration);
                _connectionMultiplexer.ConnectionFailed += MuxerConnectionFailed;
                _connectionMultiplexer.ErrorMessage += MuxerErrorMessage;
                _databases = new ConcurrentDictionary<int, IDatabase>();
            }
        }

        public IDatabase GetDatabase(int db = -1)
        {
            if (_databases.ContainsKey(db))
                return _databases[db];
            lock (_lock)
            {
                if (_databases.ContainsKey(db))
                    return _databases[db];

                var database = _connectionMultiplexer.GetDatabase(db);
                _databases.TryAdd(db, database);
                return database;
            }
        }

        #region String
        public T Get<T>(string key, int db = -1)
        {
            return Deserialize<T>(GetDatabase(db).StringGet(key));
        }

        public object Get(string key, int db = -1)
        {
            return Deserialize<object>(GetDatabase(db).StringGet(key));
        }

        public async Task<object> GetAsync(string key, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await GetDatabase(db).StringGetAsync(key);
        }

        public void Set(string key, object value, int db = -1)
        {
            GetDatabase(db).StringSet(key, Serialize(value));
        }

        public async Task SetAsync(string key, object value, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await GetDatabase(db).StringSetAsync(key, Serialize(value));
        }

        public bool SetExpireTime(string key, DateTime datetime, int db = -1)
        {
            return GetDatabase(db).KeyExpire(key, datetime);
        }

        public bool Exists(string key, int db = -1)
        {
            return GetDatabase(db).KeyExists(key);
        }

        public bool Remove(string key, int db = -1)
        {
            return GetDatabase(db).KeyDelete(key);
        }

        public async Task RemoveAsync(string key, int db = -1, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await GetDatabase(db).KeyDeleteAsync(key);
        }

        private byte[] Serialize(object value)
        {
            if (value == null)
                return null;
            var binaryFormatter = new BinaryFormatter();
            using var memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, value);
            var objectDataAsStream = memoryStream.ToArray();
            return objectDataAsStream;
        }

        private T Deserialize<T>(byte[] stream)
        {
            if (stream == null)
                return default;
            var binaryFormatter = new BinaryFormatter();
            using var memoryStream = new MemoryStream(stream);
            var result = (T)binaryFormatter.Deserialize(memoryStream);
            return result;
        }

        #endregion

        public long Increment(string key, int db = -1)
        {
            //三种命令模式
            //Sync,同步模式会直接阻塞调用者，但是显然不会阻塞其他线程。
            //Async,异步模式直接走的是Task模型。
            //Fire - and - Forget,就是发送命令，然后完全不关心最终什么时候完成命令操作。
            //即发即弃：通过配置 CommandFlags 来实现即发即弃功能，在该实例中该方法会立即返回，如果是string则返回null 如果是int则返回0.这个操作将会继续在后台运行，一个典型的用法页面计数器的实现：
            return GetDatabase(db).StringIncrement(key, flags: CommandFlags.FireAndForget);
        }

        public long Decrement(string key, string value, int db = -1)
        {
            return GetDatabase(db).HashDecrement(key, value, flags: CommandFlags.FireAndForget);
        }

        #region List
        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public long ListLeftPush(string redisKey, string redisValue, int db = -1)
        {
            return GetDatabase(db).ListLeftPush(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public long ListRightPush(string redisKey, string redisValue, int db = -1)
        {
            return GetDatabase(db).ListRightPush(redisKey, redisValue);
        }

        /// <summary>
        /// 在列表尾部插入数组集合。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="redisValue"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public long ListRightPush(string redisKey, IEnumerable<string> redisValue, int db = -1)
        {
            var items = new List<RedisValue>();
            foreach (var item in redisValue)
            {
                items.Add(item);
            }
            return GetDatabase(db).ListRightPush(redisKey, items.ToArray());
        }

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素  反序列化
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public T ListLeftPop<T>(string redisKey, int db = -1) where T : class
        {
            return NewtonsoftJsonSerializer.ToObject<T>(GetDatabase(db).ListLeftPop(redisKey));
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素   反序列化
        /// 只能是对象集合
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public T ListRightPop<T>(string redisKey, int db = -1) where T : class
        {
            return NewtonsoftJsonSerializer.ToObject<T>(GetDatabase(db).ListRightPop(redisKey));
        }

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素   
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public string ListLeftPop(string redisKey, int db = -1)
        {
            return GetDatabase(db).ListLeftPop(redisKey);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素   
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public string ListRightPop(string redisKey, int db = -1)
        {
            return GetDatabase(db).ListRightPop(redisKey);
        }

        /// <summary>
        /// 列表长度
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public long ListLength(string redisKey, int db = -1)
        {
            return GetDatabase(db).ListLength(redisKey);
        }

        /// <summary>
        /// 返回在该列表上键所对应的元素
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public IEnumerable<string> ListRange(string redisKey, int db = -1)
        {
            var result = GetDatabase(db).ListRange(redisKey);
            return result.Select(o => o.ToString());
        }

        /// <summary>
        /// 根据索引获取指定位置数据
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public IEnumerable<string> ListRange(string redisKey, int start, int stop, int db = -1)
        {
            var result = GetDatabase(db).ListRange(redisKey, start, stop);
            return result.Select(o => o.ToString());
        }

        /// <summary>
        /// 删除List中的元素 并返回删除的个数
        /// </summary>
        /// <param name="redisKey">key</param>
        /// <param name="redisValue">元素</param>
        /// <param name="type">大于零 : 从表头开始向表尾搜索，小于零 : 从表尾开始向表头搜索，等于零：移除表中所有与 VALUE 相等的值</param>
        /// <param name="db"></param>
        /// <returns></returns>
        public long ListRemove(string redisKey, string redisValue, long type = 0, int db = -1)
        {
            return GetDatabase(db).ListRemove(redisKey, redisValue, type);
        }

        /// <summary>
        /// 清空List
        /// </summary>
        /// <param name="redisKey"></param>
        /// <param name="db"></param>
        public void ListClear(string redisKey, int db = -1)
        {
            GetDatabase(db).ListTrim(redisKey, 1, 0);
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
            _logger.LogWarning("physical connection fails,try connection");
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
            _logger.LogWarning("physical connection fails,try connection");
            TryConnection();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _connectionMultiplexer?.Dispose();
            _databases?.Clear();
            if (_timer == null)
                return;
            _timer.Dispose();
            _timer = null;
        }

    }
}
