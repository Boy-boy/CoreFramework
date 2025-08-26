using StackExchange.Redis;

namespace Core.Redis
{
    public interface IRedisCache : IDisposable
    {
        /// <summary>
        /// 获取指定的redis数据库
        /// </summary>
        /// <param name="db">默认-1，范围0-15</param>
        /// <returns></returns>
        IDatabase GetDatabase(int db = -1);

        #region String
        T Get<T>(string key, int db = -1);

        Task<T> GetAsync<T>(string key, int db = -1, CancellationToken cancellationToken = default);

        void Set(string key, object value, TimeSpan? expiry = null, int db = -1);

        Task SetAsync(string key, object value, TimeSpan? expiry = null, int db = -1, CancellationToken cancellationToken = default);

        bool SetExpireTime(string key, DateTime datetime, int db = -1);

        Task<bool> SetExpireTimeAsync(string key, DateTime datetime, int db = -1, CancellationToken cancellationToken = default);

        bool Exists(string key, int db = -1);

        Task<bool> ExistsAsync(string key, int db = -1, CancellationToken cancellationToken = default);

        bool Remove(string key, int db = -1);

        Task RemoveAsync(string key, int db = -1, CancellationToken cancellationToken = default);
        #endregion

        /// <summary>
        /// 原子递增
        /// </summary>
        /// <param name="key"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long Increment(string key, int db = -1);

        /// <summary>
        /// 原子递减
        /// </summary>
        /// <param name="key"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long Decrement(string key, int db = -1);

        /// <summary>
        /// 尝试获取分布式锁
        /// </summary>
        /// <param name="lockKey"></param>
        /// <param name="expiry"></param>
        /// <param name="clientId"></param>
        /// <param name="retryCount"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        bool TryAcquireLock(string lockKey, string clientId, TimeSpan expiry, int retryCount = 3, int db = -1);

        /// <summary>
        /// 删除分布式锁
        /// </summary>
        /// <param name="lockKey"></param>
        /// <param name="clientId"></param>
        /// <param name="db"></param>
        void ReleaseLock(string lockKey, string clientId, int db = -1);

        #region List
        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListLeftPush(string key, object value, int db = -1);

        /// <summary>
        /// 在列表头部插入数组集合。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListLeftPush(string key, IEnumerable<object> value, int db = -1);

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListRightPush(string key, object value, int db = -1);

        /// <summary>
        /// 在列表尾部插入数组集合。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListRightPush(string key, IEnumerable<object> value, int db = -1);

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        T ListLeftPop<T>(string key, int db = -1);

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        T ListRightPop<T>(string key, int db = -1);

        /// <summary>
        /// 列表长度
        /// </summary>
        /// <param name="key"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListLength(string key, int db = -1);

        /// <summary>
        /// 返回在该列表上键所对应的元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        IEnumerable<T> ListRange<T>(string key, int db = -1);

        /// <summary>
        /// 根据索引获取指定位置数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        IEnumerable<T> ListRange<T>(string key, int start, int stop, int db = -1);

        /// <summary>
        /// 删除List中的元素 并返回删除的个数
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">元素</param>
        /// <param name="count">大于零 : 从表头开始向表尾搜索，小于零 : 从表尾开始向表头搜索，等于零：移除表中所有与 VALUE 相等的值</param>
        /// <param name="db"></param>
        /// <returns></returns>
        long ListRemove(string key, object value, long count = 0, int db = -1);

        /// <summary>
        /// 清空List
        /// </summary>
        /// <param name="key"></param>
        /// <param name="db"></param>
        void ListClear(string key, int db = -1);
        #endregion

        #region Hash
        /// <summary>
        /// 设置哈希字段的值
        /// </summary>
        /// <param name="key">哈希键</param>
        /// <param name="field">字段名</param>
        /// <param name="value">字段值</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        bool HashSet(string key, string field, object value, int db = -1);

        /// <summary>
        /// 获取哈希字段的值
        /// </summary>
        /// <param name="key">哈希键</param>
        /// <param name="field">字段名</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        T HashGet<T>(string key, string field, int db = -1);

        /// <summary>
        /// 获取哈希中所有字段和值
        /// </summary>
        /// <param name="key">哈希键</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        Dictionary<string, T> HashGetAll<T>(string key, int db = -1);

        /// <summary>
        /// 删除哈希中的一个或多个字段
        /// </summary>
        /// <param name="key">哈希键</param>
        /// <param name="fields">字段名</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        long HashDelete(string key, IEnumerable<string> fields, int db = -1);

        /// <summary>
        /// 判断哈希中是否存在指定字段
        /// </summary>
        /// <param name="key">哈希键</param>
        /// <param name="field">字段名</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        bool HashExists(string key, string field, int db = -1);

        /// <summary>
        /// 获取哈希中所有字段名
        /// </summary>
        /// <param name="key">哈希键</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        IEnumerable<string> HashKeys(string key, int db = -1);

        /// <summary>
        /// 获取哈希中所有字段值
        /// </summary>
        /// <param name="key">哈希键</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        IEnumerable<T> HashValues<T>(string key, int db = -1);

        /// <summary>
        /// 获取哈希中字段的数量
        /// </summary>
        /// <param name="key">哈希键</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        long HashLength(string key, int db = -1);
        #endregion

        #region Set
        /// <summary>
        /// 向集合中添加一个或多个成员
        /// </summary>
        /// <param name="key">集合键</param>
        /// <param name="values">成员值</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        long SetAdd(string key, IEnumerable<object> values, int db = -1);

        /// <summary>
        /// 获取集合中的所有成员
        /// </summary>
        /// <param name="key">集合键</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        IEnumerable<T> SetMembers<T>(string key, int db = -1);

        /// <summary>
        /// 判断成员是否存在于集合中
        /// </summary>
        /// <param name="key">集合键</param>
        /// <param name="value">成员值</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        bool SetContains(string key, object value, int db = -1);

        /// <summary>
        /// 从集合中移除一个或多个成员
        /// </summary>
        /// <param name="key">集合键</param>
        /// <param name="values">成员值</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        long SetRemove(string key, IEnumerable<object> values, int db = -1);

        /// <summary>
        /// 获取集合的成员数量
        /// </summary>
        /// <param name="key">集合键</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        long SetLength(string key, int db = -1);
        #endregion

        #region Sorted Set
        /// <summary>
        /// 向有序集合中添加一个或多个成员
        /// </summary>
        /// <param name="key">有序集合键</param>
        /// <param name="values">成员值及其分数</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        long SortedSetAdd(string key, IEnumerable<KeyValuePair<object, double>> values, int db = -1);

        /// <summary>
        /// 获取有序集合中指定范围的成员
        /// </summary>
        /// <param name="key">有序集合键</param>
        /// <param name="start">起始位置</param>
        /// <param name="stop">结束位置</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        IEnumerable<T> SortedSetRangeByRank<T>(string key, long start, long stop, int db = -1);

        /// <summary>
        /// 获取有序集合中指定分数范围的成员
        /// </summary>
        /// <param name="key">有序集合键</param>
        /// <param name="min">最小分数</param>
        /// <param name="max">最大分数</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        IEnumerable<T> SortedSetRangeByScore<T>(string key, double min, double max, int db = -1);

        /// <summary>
        /// 获取有序集合中指定成员的分数
        /// </summary>
        /// <param name="key">有序集合键</param>
        /// <param name="member">成员值</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        double? SortedSetScore(string key, object member, int db = -1);

        /// <summary>
        /// 从有序集合中移除一个或多个成员
        /// </summary>
        /// <param name="key">有序集合键</param>
        /// <param name="members">成员值</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        long SortedSetRemove(string key, IEnumerable<object> members, int db = -1);

        /// <summary>
        /// 获取有序集合的成员数量
        /// </summary>
        /// <param name="key">有序集合键</param>
        /// <param name="db">数据库编号</param>
        /// <returns></returns>
        long SortedSetLength(string key, int db = -1);
        #endregion

        #region Stream
        /// <summary>
        /// 向流中添加一条消息
        /// </summary>
        /// <param name="streamKey">流键</param>
        /// <param name="fields">消息字段</param>
        /// <param name="messageId">消息 ID（可选，如果为空则自动生成）</param>
        /// <param name="db">数据库编号</param>
        /// <returns>消息 ID</returns>
        string StreamAdd(string streamKey, IDictionary<string, string> fields, string messageId = null, int db = -1);

        /// <summary>
        /// 读取流中的消息
        /// </summary>
        /// <param name="streamKey">流键</param>
        /// <param name="messageId">起始消息 ID</param>
        /// <param name="count">最大消息数量</param>
        /// <param name="db">数据库编号</param>
        /// <returns>消息列表</returns>
        IEnumerable<(string MessageId, IDictionary<string, string> Fields)> StreamRead(string streamKey, string messageId = "0-0", int count = 10, int db = -1);

        /// <summary>
        /// 删除流中的消息
        /// </summary>
        /// <param name="streamKey">流键</param>
        /// <param name="messageIds">消息 ID 列表</param>
        /// <param name="db">数据库编号</param>
        /// <returns>删除的消息数量</returns>
        long StreamDelete(string streamKey, IEnumerable<string> messageIds, int db = -1);

        /// <summary>
        /// 获取流的长度
        /// </summary>
        /// <param name="streamKey">流键</param>
        /// <param name="db">数据库编号</param>
        /// <returns>流的长度</returns>
        long StreamLength(string streamKey, int db = -1);

        /// <summary>
        /// 创建消费者组
        /// </summary>
        /// <param name="streamKey">流键</param>
        /// <param name="groupName">消费者组名称</param>
        /// <param name="startMessageId">起始消息 ID</param>
        /// <param name="db">数据库编号</param>
        /// <returns>是否创建成功</returns>
        bool StreamCreateConsumerGroup(string streamKey, string groupName, string startMessageId = "0-0", int db = -1);

        /// <summary>
        /// 从消费者组中读取消息
        /// </summary>
        /// <param name="streamKey">流键</param>
        /// <param name="groupName">消费者组名称</param>
        /// <param name="consumerName">消费者名称</param>
        /// <param name="count">最大消息数量</param>
        /// <param name="db">数据库编号</param>
        /// <returns>消息列表</returns>
        IEnumerable<(string MessageId, IDictionary<string, string> Fields)> StreamReadGroup(string streamKey, string groupName, string consumerName, int count = 10, int db = -1);

        /// <summary>
        /// 确认消息已被处理
        /// </summary>
        /// <param name="streamKey">流键</param>
        /// <param name="groupName">消费者组名称</param>
        /// <param name="messageIds">消息 ID 列表</param>
        /// <param name="db">数据库编号</param>
        /// <returns>确认的消息数量</returns>
        long StreamAcknowledge(string streamKey, string groupName, IEnumerable<string> messageIds, int db = -1);
        #endregion
    }
}
