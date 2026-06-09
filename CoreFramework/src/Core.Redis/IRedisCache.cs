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

        bool Set<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, int db = -1, CommandFlags flags = CommandFlags.None);

        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

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
        /// <summary>
        /// 异步尝试获取分布式锁（推荐在 async 调用链中使用，重试间隔基于 Task.Delay，不阻塞 ThreadPool）。
        /// <paramref name="clientId"/> 为持有者唯一标识，必传且不能为空：加锁与释放须使用同一 token，
        /// 否则无法保证"谁加锁谁释放"，会出现互相误删锁。为空时抛出 ArgumentException。
        /// </summary>
        Task<bool> TryAcquireLockAsync(string lockKey, string clientId, TimeSpan expiry, int retryCount = 3, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>删除分布式锁。<paramref name="clientId"/> 必须与加锁时一致，必传且不能为空（为空时抛 ArgumentException）。</summary>
        void ReleaseLock(string lockKey, string clientId, int db = -1);

        /// <summary>异步删除分布式锁。<paramref name="clientId"/> 必须与加锁时一致，必传且不能为空（为空时抛 ArgumentException）。</summary>
        Task ReleaseLockAsync(string lockKey, string clientId, int db = -1, CancellationToken cancellationToken = default);
        #endregion

        #region List
        long ListLeftPush<T>(string key, T value, int db = -1);
        /// <summary>批量左推。改名 *Range 以避免与单元素泛型重载的重载解析歧义。</summary>
        long ListLeftPushRange<T>(string key, IEnumerable<T> values, int db = -1);
        Task<long> ListLeftPushAsync<T>(string key, T value, int db = -1, CancellationToken cancellationToken = default);
        Task<long> ListLeftPushRangeAsync<T>(string key, IEnumerable<T> values, int db = -1, CancellationToken cancellationToken = default);

        long ListRightPush<T>(string key, T value, int db = -1);
        long ListRightPushRange<T>(string key, IEnumerable<T> values, int db = -1);
        Task<long> ListRightPushAsync<T>(string key, T value, int db = -1, CancellationToken cancellationToken = default);
        Task<long> ListRightPushRangeAsync<T>(string key, IEnumerable<T> values, int db = -1, CancellationToken cancellationToken = default);

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
        long ListRemove<T>(string key, T value, long count = 0, int db = -1);
        Task<long> ListRemoveAsync<T>(string key, T value, long count = 0, int db = -1, CancellationToken cancellationToken = default);

        void ListClear(string key, int db = -1);
        Task ListClearAsync(string key, int db = -1, CancellationToken cancellationToken = default);
        #endregion

        #region Hash
        bool HashSet<T>(string key, string field, T value, int db = -1);
        Task<bool> HashSetAsync<T>(string key, string field, T value, int db = -1, CancellationToken cancellationToken = default);

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
        long SetAdd<T>(string key, IEnumerable<T> values, int db = -1);
        Task<long> SetAddAsync<T>(string key, IEnumerable<T> values, int db = -1, CancellationToken cancellationToken = default);

        IEnumerable<T> SetMembers<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<IEnumerable<T>> SetMembersAsync<T>(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        bool SetContains<T>(string key, T value, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<bool> SetContainsAsync<T>(string key, T value, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        long SetRemove<T>(string key, IEnumerable<T> values, int db = -1);
        Task<long> SetRemoveAsync<T>(string key, IEnumerable<T> values, int db = -1, CancellationToken cancellationToken = default);

        long SetLength(string key, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<long> SetLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);
        #endregion

        #region Sorted Set
        long SortedSetAdd<T>(string key, IEnumerable<KeyValuePair<T, double>> values, int db = -1);
        Task<long> SortedSetAddAsync<T>(string key, IEnumerable<KeyValuePair<T, double>> values, int db = -1, CancellationToken cancellationToken = default);

        IEnumerable<T> SortedSetRangeByRank<T>(string key, long start, long stop, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<IEnumerable<T>> SortedSetRangeByRankAsync<T>(string key, long start, long stop, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        IEnumerable<T> SortedSetRangeByScore<T>(string key, double min, double max, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<IEnumerable<T>> SortedSetRangeByScoreAsync<T>(string key, double min, double max, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        double? SortedSetScore<T>(string key, T member, int db = -1, CommandFlags flags = CommandFlags.None);
        Task<double?> SortedSetScoreAsync<T>(string key, T member, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        long SortedSetRemove<T>(string key, IEnumerable<T> members, int db = -1);
        Task<long> SortedSetRemoveAsync<T>(string key, IEnumerable<T> members, int db = -1, CancellationToken cancellationToken = default);

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
