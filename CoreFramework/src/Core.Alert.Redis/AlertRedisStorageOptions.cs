namespace Core.Alert.Redis
{
    /// <summary>
    /// Redis 存储配置。
    /// </summary>
    public class AlertRedisStorageOptions
    {
        /// Redis database 索引，默认使用连接默认库。
        /// </summary>
        public int Database { get; set; } = -1;

        /// <summary>
        /// 告警状态键前缀。
        /// </summary>
        public string KeyPrefix { get; set; } = "alert:escalation:";

        /// <summary>
        /// 状态 TTL，用于在异常未恢复但业务长期不再访问时兜底清理残留状态。
        /// </summary>
        public TimeSpan StateTtl { get; set; } = TimeSpan.FromHours(36);
    }
}
