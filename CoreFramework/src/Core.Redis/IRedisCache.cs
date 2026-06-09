using StackExchange.Redis;

namespace Core.Redis
{
    /// <summary>
    /// 统一 Redis 缓存与数据结构操作的高阶抽象接口。
    /// <para>设计原则：高频简单业务（如 KV 缓存）提供全包办泛型序列化；复杂数据结构（List/Hash/Set 等）透传 <see cref="RedisValue"/> 以保证性能并留给调用方自主权。</para>
    /// </summary>
    public interface IRedisCache : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 获取指定的 Redis 数据库句柄，用于绕过本接口直接调用 StackExchange.Redis 原生 API。
        /// <para>适用于扩展调用本接口未封装的高级特性（如 Lua 脚本、Pipeline 管道、Transaction 事务、Pub/Sub 发布订阅等）。</para>
        /// </summary>
        /// <param name="db">数据库索引。默认 -1 表示使用配置中的默认 DB；显式取值范围通常为 0-15。</param>
        /// <returns>返回原生 <see cref="IDatabase"/> 实例。</returns>
        /// <exception cref="InvalidOperationException">当底层连接尚未就绪、正在初始化或处于失联断开状态时抛出。</exception>
        IDatabase GetDatabase(int db = -1);

        #region String (KV 透传 RedisValue)

        /// <summary>
        /// 读取指定 key 的原始 <see cref="RedisValue"/>（GET）。
        /// <para>键不存在时返回 <see cref="RedisValue.IsNull"/> 为 true，调用方据此判断"键不存在 vs 默认值"。</para>
        /// <para>基础类型用显式强转：<c>(int)value</c>、<c>(string)value</c> 等；POCO 对象请使用 <c>GetJsonAsync&lt;T&gt;</c> 扩展方法。</para>
        /// </summary>
        /// <param name="key">缓存键，不能为空或纯空格。</param>
        /// <param name="db">数据库索引，-1 表示默认库。</param>
        /// <param name="flags">命令配置标识（如指定 <see cref="CommandFlags.DemandReplica"/> 读取从库）。</param>
        /// <returns>原始 RedisValue；键不存在时其 <see cref="RedisValue.IsNull"/> 为 true。</returns>
        RedisValue Get(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="Get"/>
        Task<RedisValue> GetAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 写入键值对（SET）。<see cref="RedisValue"/> 对 string / 数字 / bool / byte[] 等基础类型已提供隐式转换，
        /// 直接调用 <c>SetAsync(key, 123)</c> / <c>SetAsync(key, "hello")</c> 即可；POCO 对象请使用 <c>SetJsonAsync&lt;T&gt;</c> 扩展方法。
        /// </summary>
        /// <param name="key">缓存键。</param>
        /// <param name="value">待写入的 RedisValue。</param>
        /// <param name="expiry">相对过期时间；传 <c>null</c> 表示永不过期（注意：这会主动清除该 Key 现有的 TTL 剩余时间）。</param>
        /// <param name="when">条件写入策略：<see cref="When.Always"/> 总是覆盖写、<see cref="When.NotExists"/> 仅当不存在时写（SETNX 语义）、<see cref="When.Exists"/> 仅当已存在时写。</param>
        /// <param name="db">数据库索引，-1 表示默认库。</param>
        /// <param name="flags">命令配置标识。</param>
        /// <returns>操作成功返回 <c>true</c>；因条件不满足（如 NotExists 限制且键已存在）导致写入失败则返回 <c>false</c>。</returns>
        bool Set(string key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="Set"/>
        Task<bool> SetAsync(string key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将指定 key 的过期时刻重设为一个绝对的时间点（EXPIREAT 语义）。
        /// </summary>
        /// <param name="key">缓存键。</param>
        /// <param name="datetime">目标过期的绝对时间（内部将自动转换为 Unix 时间戳戳）。</param>
        /// <param name="db">数据库索引，-1 表示默认库。</param>
        /// <returns>设置成功返回 <c>true</c>；若 key 不存在则返回 <c>false</c>。</returns>
        bool SetExpireTime(string key, DateTime datetime, int db = -1);

        /// <inheritdoc cref="SetExpireTime"/>
        Task<bool> SetExpireTimeAsync(string key, DateTime datetime, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查指定的 key 是否存在。
        /// <para>注意：此操作为轻量级探针，不会触发或刷新该 Key 现有的 TTL 过期时间。</para>
        /// </summary>
        bool Exists(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="Exists"/>
        Task<bool> ExistsAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 显式删除指定的 key。
        /// <para>注意：若 key 本就不存在，该操作是安全的，不会抛出异常，直接返回 <c>false</c>。</para>
        /// </summary>
        /// <returns>若 key 存在并成功删除返回 <c>true</c>；key 不存在返回 <c>false</c>。</returns>
        bool Remove(string key, int db = -1);

        /// <inheritdoc cref="Remove"/>
        Task<bool> RemoveAsync(string key, int db = -1, CancellationToken cancellationToken = default);

        #endregion

        #region 计数器

        /// <summary>
        /// 原子递增指定的计数器键值（INCRBY 语义）。
        /// <para>注意：若该 key 不存在，Redis 会先将其初始值视作 0，再执行递增操作。</para>
        /// </summary>
        /// <param name="key">计数器键名。</param>
        /// <param name="value">递增步长，默认值为 1。</param>
        /// <param name="db">数据库索引。</param>
        /// <returns>递增操作完成后，计数器在 Redis 服务端的新值。</returns>
        /// <exception cref="RedisServerException">当该 key 已经存在但存储的不是整数字符串（无法解析为 long）时，由 Redis 服务端抛出。</exception>
        long Increment(string key, long value = 1, int db = -1);

        /// <inheritdoc cref="Increment"/>
        Task<long> IncrementAsync(string key, long value = 1, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 原子递减指定的计数器键值（DECRBY 语义）。
        /// <para>其底层行为与边界条件同 <see cref="Increment"/> 保持对称。</para>
        /// </summary>
        long Decrement(string key, long value = 1, int db = -1);

        /// <inheritdoc cref="Decrement"/>
        Task<long> DecrementAsync(string key, long value = 1, int db = -1, CancellationToken cancellationToken = default);

        #endregion

        #region Distributed Lock

        /// <summary>
        /// 异步尝试获取分布式锁（底行走 SET NX EX 语义 + 业务层指数退避重试机制）。
        /// <para>推荐在纯 async 异步调用链中使用。重试间隔基于 <see cref="Task.Delay(int)"/>，能够避免死锁并防止阻塞 ThreadPool 线程。</para>
        /// </summary>
        /// <param name="lockKey">锁的唯一资源标识键。</param>
        /// <param name="clientId">持有者唯一标识（Token/RequestId）。加锁与释放必须使用完全一致的 clientId 才能保证“谁加锁谁释放”的安全性，从而避免误删他人持有的锁。</param>
        /// <param name="expiry">锁的最长持有寿命。超时后将由 Redis 自动剔除。此参数是防止持有锁的微服务宿主意外崩溃、宕机而导致永久死锁的关键保护线。</param>
        /// <param name="retryCount">最大尝试次数（包含首次锁争抢），必须 &gt; 0。</param>
        /// <param name="db">数据库索引。</param>
        /// <param name="cancellationToken">异步取消令牌。</param>
        /// <returns>成功抢到锁返回 <c>true</c>; 耗尽全部重试次数后仍未抢到则返回 <c>false</c>。</returns>
        /// <exception cref="ArgumentException">当 <paramref name="clientId"/> 为 null 或空字符串时抛出。</exception>
        Task<bool> TryAcquireLockAsync(string lockKey, string clientId, TimeSpan expiry, int retryCount = 3, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 释放分布式锁。
        /// <para>底层通过 Lua 脚本执行，保证“读取锁所有权 -> 比对 clientId -> 校验匹配则执行删除”这一原子组合拳，彻底阻断因网络卡顿引发的误删他人锁问题。</para>
        /// </summary>
        /// <param name="lockKey">锁的资源键。</param>
        /// <param name="clientId">加锁时传入的持有者唯一标识，不能为空。</param>
        /// <param name="db">数据库索引。</param>
        /// <exception cref="ArgumentException">当 <paramref name="clientId"/> 为 null 或空字符串时抛出。</exception>
        void ReleaseLock(string lockKey, string clientId, int db = -1);

        /// <inheritdoc cref="ReleaseLock"/>
        Task ReleaseLockAsync(string lockKey, string clientId, int db = -1, CancellationToken cancellationToken = default);

        #endregion

        #region List (透传 RedisValue)

        /// <summary>
        /// 从列表左侧推入单个元素（LPUSH）。若该列表 key 不存在，会自动创建。
        /// </summary>
        /// <returns>操作完成之后，该 List 列表的总长度。</returns>
        long ListLeftPush(string key, RedisValue value, int db = -1);

        /// <summary>
        /// 批量从列表左侧推入多个元素（LPUSH 批量版）。
        /// <para>注意：由于 Redis LPUSH 命令的行为特性，数组中靠前的元素最终会排在列表的最前端（表头）。</para>
        /// </summary>
        /// <returns>批量操作完成之后，该 List 列表的总长度。</returns>
        long ListLeftPushRange(string key, RedisValue[] values, int db = -1);

        /// <inheritdoc cref="ListLeftPush"/>
        Task<long> ListLeftPushAsync(string key, RedisValue value, int db = -1, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ListLeftPushRange"/>
        Task<long> ListLeftPushRangeAsync(string key, RedisValue[] values, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>从列表右侧推入单个元素（RPUSH）。其语义与边界表现与 <see cref="ListLeftPush"/> 完全对称。</summary>
        long ListRightPush(string key, RedisValue value, int db = -1);

        /// <summary>批量从列表右侧推入多个元素（RPUSH 批量版）。</summary>
        long ListRightPushRange(string key, RedisValue[] values, int db = -1);

        /// <inheritdoc cref="ListRightPush"/>
        Task<long> ListRightPushAsync(string key, RedisValue value, int db = -1, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ListRightPushRange"/>
        Task<long> ListRightPushRangeAsync(string key, RedisValue[] values, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 从列表左侧弹出一个元素（LPOP）。
        /// <para>核心提示：当队列为空或键不存在时，返回 <see cref="RedisValue.Null"/>。调用方在后续执行转型或反序列化前，必须先通过 <see cref="RedisValue.IsNull"/> 进行非空防御校验。</para>
        /// </summary>
        /// <returns>弹出的元素内容；若队列已空返回 <see cref="RedisValue.Null"/>。</returns>
        RedisValue ListLeftPop(string key, int db = -1);

        /// <inheritdoc cref="ListLeftPop"/>
        Task<RedisValue> ListLeftPopAsync(string key, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>从列表右侧弹出一个元素（RPOP）。其边界防御行为与 <see cref="ListLeftPop"/> 完全一致。</summary>
        RedisValue ListRightPop(string key, int db = -1);

        /// <inheritdoc cref="ListRightPop"/>
        Task<RedisValue> ListRightPopAsync(string key, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>获取列表的当前长度（LLEN）。若 key 不存在则视为安全状态并返回 0。</summary>
        long ListLength(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="ListLength"/>
        Task<long> ListLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 一次性读取并返回整个列表的所有元素（LRANGE 0 -1）。
        /// <para>警告：此方法的时间复杂度为 O(N)。严禁在大型列表（大 Key）上调用，否则会引发 Redis 线程阻塞和内存网络暴涨。</para>
        /// </summary>
        RedisValue[] ListRange(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <summary>
        /// 根据指定的索引闭区间范围读取列表元素（LRANGE）。
        /// </summary>
        /// <param name="key">列表键。</param>
        /// <param name="start">起始索引。支持负数（例如：-1 表示列表最后一个元素，-2 表示倒数第二个）。</param>
        /// <param name="stop">结束索引（包含）。支持负数。</param>
        /// <param name="db">数据库索引。</param>
        /// <param name="flags">命令配置标识。</param>
        /// <returns>符合索引区间要求的元素数组；若 key 不存在则返回空数组。</returns>
        RedisValue[] ListRange(string key, int start, int stop, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="ListRange(string, int, CommandFlags)"/>
        Task<RedisValue[]> ListRangeAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="ListRange(string, int, int, int, CommandFlags)"/>
        Task<RedisValue[]> ListRangeAsync(string key, int start, int stop, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 从列表中删除与指定 <paramref name="value"/> 相等的元素（LREM）。
        /// </summary>
        /// <param name="key">列表键。</param>
        /// <param name="value">比对的目标元素值。</param>
        /// <param name="count">移除策略。当 &gt;0 时：从表头向表尾搜索并移除最多 count 个；当 &lt;0 时：从表尾向表头反向搜索并移除最多 |count| 个；当 =0 时：移除列表中所有匹配的元素。</param>
        /// <param name="db">数据库索引。</param>
        /// <returns>实际在 Redis 服务端被成功删除的匹配元素总个数。</returns>
        long ListRemove(string key, RedisValue value, long count = 0, int db = -1);

        /// <inheritdoc cref="ListRemove"/>
        Task<long> ListRemoveAsync(string key, RedisValue value, long count = 0, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清空整个列表。
        /// <para>实现机制：底层通过执行反向区间的 <c>LTRIM 0 -1</c> 实现。其物理副作用与直接执行 DEL 相当，但对于不存在的 list 也是绝对安全的。</para>
        /// </summary>
        void ListClear(string key, int db = -1);

        /// <inheritdoc cref="ListClear"/>
        Task ListClearAsync(string key, int db = -1, CancellationToken cancellationToken = default);

        #endregion

        #region Hash (透传 RedisValue / HashEntry)

        /// <summary>
        /// 设置 Hash 哈希表中的单个字段值（HSET）。
        /// </summary>
        /// <returns>如果在哈希表中新建了该字段，返回 <c>true</c>；如果是对已有字段的旧值进行了覆盖，则返回 <c>false</c>。</returns>
        bool HashSet(string key, string field, RedisValue value, int db = -1);

        /// <inheritdoc cref="HashSet"/>
        Task<bool> HashSetAsync(string key, string field, RedisValue value, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取哈希表中指定字段的值（HGET）。
        /// <para>注意：若该哈希表 key 不存在，或者字段 field 不存在，将返回 <see cref="RedisValue.Null"/>。</para>
        /// </summary>
        RedisValue HashGet(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="HashGet"/>
        Task<RedisValue> HashGetAsync(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取并返回整个哈希表的所有字段与值（HGETALL）。直接透传 SE.Redis 原生的 <see cref="HashEntry"/>[] 以避免二次分配。
        /// <para>警告：时间复杂度为 O(N)。严禁在海量字段的大型 Hash 上使用该操作，否则会导致 Redis 阻塞。</para>
        /// </summary>
        HashEntry[] HashGetAll(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="HashGetAll"/>
        Task<HashEntry[]> HashGetAllAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量删除哈希表中的指定字段（HDEL）。
        /// <para>调用方常态可直接用 collection expression 字面量（如 <c>["name", "age"]</c>），
        /// 编译器会借助 <see cref="RedisValue"/> 对 string 的隐式转换自动构造数组，
        /// 无须显式 <c>new RedisValue[] {...}</c>。</para>
        /// </summary>
        /// <returns>实际被成功删除的字段数量（注：传入但本就不存在的字段不会被计入）。</returns>
        long HashDelete(string key, RedisValue[] fields, int db = -1);

        /// <inheritdoc cref="HashDelete"/>
        Task<long> HashDeleteAsync(string key, RedisValue[] fields, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>判断哈希表中是否存在指定字段（HEXISTS）。若哈希表 key 或是字段 field 任一不存在，均返回 <c>false</c>。</summary>
        bool HashExists(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="HashExists"/>
        Task<bool> HashExistsAsync(string key, string field, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取哈希表内所有的字段名列表（HKEYS）。
        /// <para>性能警告：O(N) 操作，请勿对大 Hash 键使用。</para>
        /// </summary>
        RedisValue[] HashKeys(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="HashKeys"/>
        Task<RedisValue[]> HashKeysAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取哈希表内所有的字段值列表（HVALS）。
        /// <para>性能警告：O(N) 操作，请勿对大 Hash 键使用。</para>
        /// </summary>
        RedisValue[] HashValues(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="HashValues"/>
        Task<RedisValue[]> HashValuesAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>获取哈希表中的字段总数量（HLEN）。若哈希表 key 物理上不存在，返回 0。</summary>
        long HashLength(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="HashLength"/>
        Task<long> HashLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        #endregion

        #region Set (透传 RedisValue)

        /// <summary>
        /// 批量将成员加入 Set 无序集合（SADD）。
        /// </summary>
        /// <returns>本次操作实际新增进集合的数组成员个数（集合中已存在的成员会自动忽略，不计入返回值）。</returns>
        long SetAdd(string key, RedisValue[] values, int db = -1);

        /// <inheritdoc cref="SetAdd"/>
        Task<long> SetAddAsync(string key, RedisValue[] values, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取 Set 无序集合中的全部成员（SMEMBERS）。
        /// <para>警告：时间复杂度 O(N)。大集合在生产环境高频遍历时，强烈建议绕过本方法改用原生的 <c>SSCAN</c> 游标分批读取。</para>
        /// </summary>
        RedisValue[] SetMembers(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="SetMembers"/>
        Task<RedisValue[]> SetMembersAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>判断指定元素是否是 Set 集合的成员（SISMEMBER）。时间复杂度为 O(1)。</summary>
        bool SetContains(string key, RedisValue value, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="SetContains"/>
        Task<bool> SetContainsAsync(string key, RedisValue value, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 从 Set 集合中批量移除指定成员（SREM）。
        /// </summary>
        /// <returns>实际被成功移除的非重复数组成员个数。</returns>
        long SetRemove(string key, RedisValue[] values, int db = -1);

        /// <inheritdoc cref="SetRemove"/>
        Task<long> SetRemoveAsync(string key, RedisValue[] values, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>获取 Set 集合的基数/成员总数（SCARD）。若集合 key 不存在则返回 0。</summary>
        long SetLength(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="SetLength"/>
        Task<long> SetLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        #endregion

        #region Sorted Set (透传 SortedSetEntry / RedisValue)

        /// <summary>
        /// 批量向 ZSet 有序集合中添加成员及其分数（ZADD）。
        /// <para>注意：若成员在集合中已存在，其分数（Score）会被覆盖更新为新值，但此项更新不会计入新增返回值中。</para>
        /// </summary>
        /// <returns>实际成功新添加进有序集合的非重复元素成员数量。</returns>
        long SortedSetAdd(string key, SortedSetEntry[] entries, int db = -1);

        /// <inheritdoc cref="SortedSetAdd"/>
        Task<long> SortedSetAddAsync(string key, SortedSetEntry[] entries, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按照成员的分数（Score）排名进行【升序】闭区间范围读取（ZRANGE）。
        /// </summary>
        /// <param name="key">有序集合键。</param>
        /// <param name="start">起始排名（从 0 开始）。支持负数索引（如 -1 代表最后一个，-2 代表倒数第二个）。</param>
        /// <param name="stop">结束排名（包含）。支持负数索引。</param>
        /// <param name="db">数据库索引。</param>
        /// <param name="flags">命令配置标识。</param>
        /// <returns>符合排名区间段的数组成员。</returns>
        RedisValue[] SortedSetRangeByRank(string key, long start, long stop, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="SortedSetRangeByRank"/>
        Task<RedisValue[]> SortedSetRangeByRankAsync(string key, long start, long stop, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按照分数（Score）的闭区间范围进行【升序】读取（ZRANGEBYSCORE）。
        /// </summary>
        /// <param name="key">有序集合键。</param>
        /// <param name="min">分数下界。如需表达负无穷无下界，请传 <see cref="double.NegativeInfinity"/>。</param>
        /// <param name="max">分数上界。如需表达正无穷无上界，请传 <see cref="double.PositiveInfinity"/>。</param>
        /// <param name="db">数据库索引。</param>
        /// <param name="flags">命令配置标识。</param>
        /// <returns>分数落在 [min, max] 区间内的数组成员。</returns>
        RedisValue[] SortedSetRangeByScore(string key, double min, double max, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="SortedSetRangeByScore"/>
        Task<RedisValue[]> SortedSetRangeByScoreAsync(string key, double min, double max, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取有序集合中指定成员的当前分数（ZSCORE）。
        /// </summary>
        /// <returns>成员存在时返回其 double 类型分数；若成员或有序集合 key 不存在，则返回 <c>null</c>。</returns>
        double? SortedSetScore(string key, RedisValue member, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="SortedSetScore"/>
        Task<double?> SortedSetScoreAsync(string key, RedisValue member, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量从有序集合中移除指定的成员（ZREM）。
        /// </summary>
        /// <returns>实际在 Redis 端被成功移除的成员数量。</returns>
        long SortedSetRemove(string key, RedisValue[] members, int db = -1);

        /// <inheritdoc cref="SortedSetRemove"/>
        Task<long> SortedSetRemoveAsync(string key, RedisValue[] members, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>获取有序集合的成员总数（ZCARD）。若集合 key 不存在则返回 0。</summary>
        long SortedSetLength(string key, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="SortedSetLength"/>
        Task<long> SortedSetLengthAsync(string key, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        #endregion

        #region Stream (透传 NameValueEntry / StreamEntry)

        /// <summary>
        /// 向指定的 Stream 消息流追加一条新消息（XADD）。
        /// </summary>
        /// <param name="streamKey">Stream 消息流的键名。</param>
        /// <param name="fields">消息的键值对字段数组。</param>
        /// <param name="messageId">可选的显式消息 ID。默认传 <c>null</c> 由 Redis 自动生成（格式为“时间戳-自增序号”，天然单调递增）；若显式传入，格式必须严格遵循 <c>毫秒数-序号</c> 规范，且新 ID 必须大于当前流中最新一条消息的 ID。</param>
        /// <param name="db">数据库索引。</param>
        /// <returns>实际写入流后的消息唯一 ID 字符串（例如 "1625000000000-0"）。</returns>
        string StreamAdd(string streamKey, NameValueEntry[] fields, string messageId = null, int db = -1);

        /// <inheritdoc cref="StreamAdd"/>
        Task<string> StreamAddAsync(string streamKey, NameValueEntry[] fields, string messageId = null, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 基础流读取：从指定的独占消息 ID（不包含自身）之后，顺序拉取最多 <paramref name="count"/> 条增量消息（XREAD）。
        /// <para>常用场景：传 <c>"0-0"</c> 表示从流的最初起点无遗漏消费；在持续消费循环中，每次传入上一次读到的最后一条消息 ID 即可实现标准的增量迭代。</para>
        /// </summary>
        /// <returns>符合读取范围的消息条目数组；若没有新消息则返回空数组。</returns>
        StreamEntry[] StreamRead(string streamKey, RedisValue messageId, int count = 10, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="StreamRead"/>
        Task<StreamEntry[]> StreamReadAsync(string streamKey, RedisValue messageId, int count = 10, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量物理删除 Stream 流中的指定消息（XDEL）。
        /// <para>核心提示：这通常用来对单条异常破坏性数据进行特例擦除。若要缩减流的总大小，通常不建议频繁使用 XDEL，建议直接在 XADD 时指定 MAXLEN 触发流裁剪修剪。</para>
        /// </summary>
        /// <returns>实际被成功删除的消息物理计数。</returns>
        long StreamDelete(string streamKey, RedisValue[] messageIds, int db = -1);

        /// <inheritdoc cref="StreamDelete"/>
        Task<long> StreamDeleteAsync(string streamKey, RedisValue[] messageIds, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>获取 Stream 消息流当前的实际消息总长度（XLEN）。若 key 不存在则返回 0。</summary>
        long StreamLength(string streamKey, int db = -1, CommandFlags flags = CommandFlags.None);

        /// <inheritdoc cref="StreamLength"/>
        Task<long> StreamLengthAsync(string streamKey, int db = -1, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

        /// <summary>
        /// 显式为 Stream 创建持久化的协同消费组（XGROUP CREATE）。
        /// </summary>
        /// <param name="streamKey">Stream 消息流的键名。</param>
        /// <param name="groupName">消费组名称（建议全系统唯一）。</param>
        /// <param name="startMessageId">消费的起始定位：常用传 <c>"0-0"</c> 表示该组从头消费流内的全量历史数据；传 <c>"$"</c> 表示该消费组建立后，只认准并消费此后新追加进来的消息。</param>
        /// <param name="db">数据库索引。</param>
        /// <returns>创建成功返回 <c>true</c>。</returns>
        /// <exception cref="RedisServerException">当该同名消费组在 Redis 侧已客观存在时，服务端会抛出 <c>BUSYGROUP</c> 异常。调用方通常需要显式 catch 此异常并识别其异常文本，以支持系统的幂等性初始化启动流程。</exception>
        bool StreamCreateConsumerGroup(string streamKey, string groupName, RedisValue startMessageId, int db = -1);

        /// <inheritdoc cref="StreamCreateConsumerGroup"/>
        Task<bool> StreamCreateConsumerGroupAsync(string streamKey, string groupName, RedisValue startMessageId, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 以特定消费组身份，拉取指派给当前消费者实例的新消息（XREADGROUP GROUP ... &gt; 语义）。
        /// <para>关键机制：该方法拉取到的消息，在业务显式对其确认前，都会被强制移存入该消费者独享的 PEL（Pending Entries List，待确认列表）中。调用方处理完业务后必须手动调用 <see cref="StreamAcknowledge"/> 做出 ACK 响应，否则消息会死死挂载在 PEL 中造成内存堆积，且无法被消费组内其他兄弟实例接管补偿。</para>
        /// </summary>
        /// <param name="streamKey">Stream 消息流的键名。</param>
        /// <param name="groupName">消费组名称。</param>
        /// <param name="consumerName">消费者实例名（建议使用“主机名+进程ID”或容器 PodID 充当），用于隔离消费组内多节点竞争时的 PEL 归属。</param>
        /// <param name="count">单次最大拉取的消息条目条数限制。</param>
        /// <param name="db">数据库索引。</param>
        /// <returns>消费组分配给此消费者的消息条目数组。</returns>
        StreamEntry[] StreamReadGroup(string streamKey, string groupName, string consumerName, int count = 10, int db = -1);

        /// <inheritdoc cref="StreamReadGroup"/>
        Task<StreamEntry[]> StreamReadGroupAsync(string streamKey, string groupName, string consumerName, int count = 10, int db = -1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 对消费组中的消息确认其已正确处理完毕（XACK 命令），将其从 PEL 待确认列表中永久移除。
        /// </summary>
        /// <param name="streamKey">Stream 消息流的键名。</param>
        /// <param name="groupName">消费组名称。</param>
        /// <param name="messageIds">待确认的消息 ID 数组。</param>
        /// <param name="db">数据库索引。</param>
        /// <returns>实际在 Redis 服务端被成功 ACK 确认并移出 PEL 列表的消息计数值（已被他人 ACK 或本就不存在的消息 ID 不会计入）。</returns>
        long StreamAcknowledge(string streamKey, string groupName, RedisValue[] messageIds, int db = -1);

        /// <inheritdoc cref="StreamAcknowledge"/>
        Task<long> StreamAcknowledgeAsync(string streamKey, string groupName, RedisValue[] messageIds, int db = -1, CancellationToken cancellationToken = default);

        #endregion
    }
}