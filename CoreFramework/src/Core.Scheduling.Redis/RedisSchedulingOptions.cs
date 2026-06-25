namespace Core.Scheduling.Redis
{
    /// <summary>
    /// Redis 分布式锁适配器配置。
    /// 锁租约时长读自通用 <see cref="Core.Scheduling.Hosting.SchedulingOptions.DistributedLockLeaseDuration"/>;
    /// 本对象只承载 Redis 连接 / Key 命名 / DB 选择。
    /// </summary>
    public sealed class RedisSchedulingOptions
    {
        /// <summary>
        /// Redis 连接串。<see langword="null"/> / 空时,适配器期望 DI 中已注册
        /// <see cref="global::StackExchange.Redis.IConnectionMultiplexer"/>(由 <c>Core.Redis</c> 或调用方自行注册)。
        /// 非空则本包用此连接串建一个 <see cref="global::StackExchange.Redis.IConnectionMultiplexer"/> 单例。
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Redis Key 前缀。同一 Redis 实例支撑多套应用时,用前缀互相隔离。
        /// 完整 Key:<c>{KeyPrefix}{HandlerCode}</c>。
        /// </summary>
        public string KeyPrefix { get; set; } = "core-scheduling:lock:";

        /// <summary>
        /// 使用的 Redis 数据库编号;默认 0。
        /// </summary>
        public int Database { get; set; } = 0;
    }
}
