using StackExchange.Redis;

namespace Core.Redis
{
    public interface IRedisCache : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 获取指定的 Redis 数据库
        /// </summary>
        /// <param name="db">默认 -1，范围 0-15</param>
        IDatabase GetDatabase(int db = -1);

        #region String
        T Get<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        Task<T> GetAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        bool Set(string key, object value, TimeSpan? expiry = null, When when = When.Always, int db = -1, CommandFlags flags = CommandFlags.None);

        Task<bool> SetAsync(string key, object value, TimeSpan? expiry = null, When when = When.Always, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        bool SetExpireTime(string key, DateTime datetime, int db = -1);

        Task<bool> SetExpireTimeAsync(string key, DateTime datetime, int db = -1, CancellationToken cancellationToken = default);

        bool Exists(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        Task<bool> ExistsAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        bool Remove(string key, int db = -1);

        Task<bool> RemoveAsync(string key, int db = -1, CancellationToken cancellationToken = default);
        #endregion

        #region Counters
        /// <summary>原子递增</summary>
        long Increment(string key, long value = 1, int db = -1);

        Task<long> IncrementAsync(string key, long value = 1, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>原子递减</summary>
        long Decrement(string key, long value = 1, int db = -1);

        Task<long> DecrementAsync(string key, long value = 1, int db = -1, CancellationToken cancellationToken = default);
        #endregion

        #region Distributed Lock
        /// <summary>异步尝试获取分布式锁（推荐在 async 调用链中使用，重试间隔基于 Task.Delay，不阻塞 ThreadPool）</summary>
        Task<bool> TryAcquireLockAsync(string lockKey, string clientId, TimeSpan expiry, int retryCount = 3, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>删除分布式锁</summary>
        void ReleaseLock(string lockKey, string clientId, int db = -1);

        /// <summary>异步删除分布式锁</summary>
        Task ReleaseLockAsync(string lockKey, string clientId, int db = -1, CancellationToken cancellationToken = default);
        #endregion

        #region List
        long ListLeftPush(string key, object value, int db = -1);
        long ListLeftPush(string key, IEnumerable<object> value, int db = -1);
        Task<long> ListLeftPushAsync(string key, object value, int db = -1, CancellationToken cancellationToken = default);
        Task<long> ListLeftPushAsync(string key, IEnumerable<object> value, int db = -1, CancellationToken cancellationToken = default);

        long ListRightPush(string key, object value, int db = -1);
        long ListRightPush(string key, IEnumerable<object> value, int db = -1);
        Task<long> ListRightPushAsync(string key, object value, int db = -1, CancellationToken cancellationToken = default);
        Task<long> ListRightPushAsync(string key, IEnumerable<object> value, int db = -1, CancellationToken cancellationToken = default);

        T ListLeftPop<T>(string key, int db = -1);
        Task<T> ListLeftPopAsync<T>(string key, int db = -1, CancellationToken cancellationToken = default);

        T ListRightPop<T>(string key, int db = -1);
        Task<T> ListRightPopAsync<T>(string key, int db = -1, CancellationToken cancellationToken = default);

        long ListLength(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<long> ListLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        IEnumerable<T> ListRange<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        IEnumerable<T> ListRange<T>(string key, int start, int stop, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<IEnumerable<T>> ListRangeAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> ListRangeAsync<T>(string key, int start, int stop, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除 List 中的元素 并返回删除的个数
        /// </summary>
        /// <param name="count">&gt;0 从表头向表尾搜索；&lt;0 从表尾向表头；=0 移除所有匹配</param>
        long ListRemove(string key, object value, long count = 0, int db = -1);
        Task<long> ListRemoveAsync(string key, object value, long count = 0, int db = -1, CancellationToken cancellationToken = default);

        void ListClear(string key, int db = -1);
        Task ListClearAsync(string key, int db = -1, CancellationToken cancellationToken = default);
        #endregion

        #region Hash
        bool HashSet(string key, string field, object value, int db = -1);
        Task<bool> HashSetAsync(string key, string field, object value, int db = -1, CancellationToken cancellationToken = default);

        T HashGet<T>(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<T> HashGetAsync<T>(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        Dictionary<string, T> HashGetAll<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<Dictionary<string, T>> HashGetAllAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        long HashDelete(string key, IEnumerable<string> fields, int db = -1);
        Task<long> HashDeleteAsync(string key, IEnumerable<string> fields, int db = -1, CancellationToken cancellationToken = default);

        bool HashExists(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<bool> HashExistsAsync(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        IEnumerable<string> HashKeys(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<IEnumerable<string>> HashKeysAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        IEnumerable<T> HashValues<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<IEnumerable<T>> HashValuesAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        long HashLength(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<long> HashLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);
        #endregion

        #region Set
        long SetAdd(string key, IEnumerable<object> values, int db = -1);
        Task<long> SetAddAsync(string key, IEnumerable<object> values, int db = -1, CancellationToken cancellationToken = default);

        IEnumerable<T> SetMembers<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<IEnumerable<T>> SetMembersAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        bool SetContains(string key, object value, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<bool> SetContainsAsync(string key, object value, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        long SetRemove(string key, IEnumerable<object> values, int db = -1);
        Task<long> SetRemoveAsync(string key, IEnumerable<object> values, int db = -1, CancellationToken cancellationToken = default);

        long SetLength(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<long> SetLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);
        #endregion

        #region Sorted Set
        long SortedSetAdd(string key, IEnumerable<KeyValuePair<object, double>> values, int db = -1);
        Task<long> SortedSetAddAsync(string key, IEnumerable<KeyValuePair<object, double>> values, int db = -1, CancellationToken cancellationToken = default);

        IEnumerable<T> SortedSetRangeByRank<T>(string key, long start, long stop, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<IEnumerable<T>> SortedSetRangeByRankAsync<T>(string key, long start, long stop, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        IEnumerable<T> SortedSetRangeByScore<T>(string key, double min, double max, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<IEnumerable<T>> SortedSetRangeByScoreAsync<T>(string key, double min, double max, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        double? SortedSetScore(string key, object member, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<double?> SortedSetScoreAsync(string key, object member, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        long SortedSetRemove(string key, IEnumerable<object> members, int db = -1);
        Task<long> SortedSetRemoveAsync(string key, IEnumerable<object> members, int db = -1, CancellationToken cancellationToken = default);

        long SortedSetLength(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<long> SortedSetLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);
        #endregion

        #region Stream
        string StreamAdd(string streamKey, IDictionary<string, string> fields, string messageId = null, int db = -1);
        Task<string> StreamAddAsync(string streamKey, IDictionary<string, string> fields, string messageId = null, int db = -1, CancellationToken cancellationToken = default);

        IEnumerable<(string MessageId, IDictionary<string, string> Fields)> StreamRead(string streamKey, string messageId = "0-0", int count = 10, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<IEnumerable<(string MessageId, IDictionary<string, string> Fields)>> StreamReadAsync(string streamKey, string messageId = "0-0", int count = 10, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        long StreamDelete(string streamKey, IEnumerable<string> messageIds, int db = -1);
        Task<long> StreamDeleteAsync(string streamKey, IEnumerable<string> messageIds, int db = -1, CancellationToken cancellationToken = default);

        long StreamLength(string streamKey, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<long> StreamLengthAsync(string streamKey, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        bool StreamCreateConsumerGroup(string streamKey, string groupName, string startMessageId = "0-0", int db = -1);
        Task<bool> StreamCreateConsumerGroupAsync(string streamKey, string groupName, string startMessageId = "0-0", int db = -1, CancellationToken cancellationToken = default);

        IEnumerable<(string MessageId, IDictionary<string, string> Fields)> StreamReadGroup(string streamKey, string groupName, string consumerName, int count = 10, int db = -1);
        Task<IEnumerable<(string MessageId, IDictionary<string, string> Fields)>> StreamReadGroupAsync(string streamKey, string groupName, string consumerName, int count = 10, int db = -1, CancellationToken cancellationToken = default);

        long StreamAcknowledge(string streamKey, string groupName, IEnumerable<string> messageIds, int db = -1);
        Task<long> StreamAcknowledgeAsync(string streamKey, string groupName, IEnumerable<string> messageIds, int db = -1, CancellationToken cancellationToken = default);
        #endregion
    }
}
