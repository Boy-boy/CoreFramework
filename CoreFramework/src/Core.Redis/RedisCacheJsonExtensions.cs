using StackExchange.Redis;
using System.Text.Json;

namespace Core.Redis
{
    /// <summary>
    /// 面向 POCO 对象的 JSON 序列化扩展。基础类型请直接使用 <see cref="IRedisCache.Get"/> / <see cref="IRedisCache.Set"/>
    /// 配合 <see cref="RedisValue"/> 自带的隐式 / 显式转换。
    /// <para>
    /// 序列化结果以 UTF-8 字节数组形式写入 Redis；反序列化优先走 <see cref="RedisValue"/> → <see cref="ReadOnlyMemory{Byte}"/> 的零拷贝路径。
    /// </para>
    /// <para>
    /// AOT/Trimming 场景请传入 source-generated <see cref="JsonSerializerOptions"/>
    /// （挂载 <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>），否则将回退反射解析。
    /// </para>
    /// </summary>
    public static class RedisCacheJsonExtensions
    {
        // 与 System.Text.Json 静态默认实例保持一致：PascalCase、不允许尾随逗号、忽略 null=false、字段不序列化
        private static readonly JsonSerializerOptions DefaultJsonOptions = JsonSerializerOptions.Default;

        /// <summary>
        /// 同步版 <see cref="SetJsonAsync{T}"/>。
        /// </summary>
        public static bool SetJson<T>(this IRedisCache cache, string key, T value,
            TimeSpan? expiry = null, When when = When.Always, int db = -1,
            CommandFlags flags = CommandFlags.None, JsonSerializerOptions options = null)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, options ?? DefaultJsonOptions);
            return cache.Set(key, bytes, expiry, when, db, flags);
        }

        /// <summary>
        /// 将 POCO 对象 JSON 序列化后写入 Redis。<paramref name="value"/> 为 <c>null</c> 时写入 JSON 字面量 "null"
        /// （非 <see cref="RedisValue.Null"/>），与 <see cref="GetJsonAsync{T}"/> 的反序列化对称。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存键。</param>
        /// <param name="value">待缓存的对象。</param>
        /// <param name="expiry">相对过期时间；<c>null</c> 表示永不过期。</param>
        /// <param name="when">条件写入策略。</param>
        /// <param name="db">数据库索引。</param>
        /// <param name="flags">命令配置标识。</param>
        /// <param name="options">自定义 JSON 配置；不传则使用 <see cref="JsonSerializerOptions.Default"/>。</param>
        /// <param name="cancellationToken">异步取消令牌。</param>
        public static Task<bool> SetJsonAsync<T>(this IRedisCache cache, string key, T value,
            TimeSpan? expiry = null, When when = When.Always, int db = -1,
            CommandFlags flags = CommandFlags.None, JsonSerializerOptions options = null,
            CancellationToken cancellationToken = default)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, options ?? DefaultJsonOptions);
            return cache.SetAsync(key, bytes, expiry, when, db, flags, cancellationToken);
        }

        /// <summary>
        /// 同步版 <see cref="GetJsonAsync{T}"/>。
        /// </summary>
        public static T GetJson<T>(this IRedisCache cache, string key,
            int db = -1, CommandFlags flags = CommandFlags.None, JsonSerializerOptions options = null)
        {
            RedisValue raw = cache.Get(key, db, flags);
            return raw.IsNull
                ? default
                : JsonSerializer.Deserialize<T>(((ReadOnlyMemory<byte>)raw).Span, options ?? DefaultJsonOptions);
        }

        /// <summary>
        /// 从 Redis 读取并 JSON 反序列化为 POCO 对象。键不存在时返回 <c>default(T)</c>。
        /// 反序列化走 <see cref="RedisValue"/> → <see cref="ReadOnlyMemory{Byte}"/>，存储为 Raw 时零拷贝。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存键。</param>
        /// <param name="db">数据库索引。</param>
        /// <param name="flags">命令配置标识。</param>
        /// <param name="options">自定义 JSON 配置；不传则使用 <see cref="JsonSerializerOptions.Default"/>。</param>
        /// <param name="cancellationToken">异步取消令牌。</param>
        /// <returns>反序列化后的对象；键不存在时返回 <c>default(T)</c>。</returns>
        public static async Task<T> GetJsonAsync<T>(this IRedisCache cache, string key,
            int db = -1, CommandFlags flags = CommandFlags.None, JsonSerializerOptions options = null,
            CancellationToken cancellationToken = default)
        {
            RedisValue raw = await cache.GetAsync(key, db, flags, cancellationToken);
            return raw.IsNull
                ? default
                : JsonSerializer.Deserialize<T>(((ReadOnlyMemory<byte>)raw).Span, options ?? DefaultJsonOptions);
        }
    }
}
