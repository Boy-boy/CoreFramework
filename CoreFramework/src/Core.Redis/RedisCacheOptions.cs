using StackExchange.Redis;

namespace Core.Redis
{
    public class RedisCacheOptions
    {
        /// <summary>
        /// 连接字符串
        /// 单机/主从/集群：host1:6379,host2:6379,...
        /// 哨兵模式：填写哨兵节点列表且必须包含 serviceName，驱动会自动连哨兵、发现主、并在故障转移时自更新，例如：
        /// "host1:26379,host2:26379,password=123,serviceName=mymaster"
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// 直接提供 StackExchange.Redis 的 ConfigurationOptions，若设置则优先生效。
        /// 设置了 ServiceName 即由驱动自动识别为哨兵模式；内部每次返回前会 Clone，外部修改不会污染缓存连接。
        /// </summary>
        public ConfigurationOptions ConfigurationOptions { get; set; }

        /// <summary>
        /// 键前缀
        /// </summary>
        public string InstancePrefix { get; set; }

        /// <summary>
        /// 连接健康检查间隔（秒），默认 60
        /// </summary>
        public int ConnectionHealthCheck { get; set; } = 60;

        /// <summary>
        /// 兜底强制重建的"持续断开"阈值（秒），默认 30。
        /// </summary>
        public int SustainedDisconnectBeforeReconnect { get; set; } = 30;

        /// <summary>
        /// 两次强制重建之间的最小间隔（秒），默认 60。
        /// </summary>
        public int MinReconnectInterval { get; set; } = 60;

        /// <summary>
        /// 默认数据库编号；-1 表示沿用客户端默认
        /// </summary>
        public int DefaultDatabase { get; set; } = -1;

        /// <summary>
        /// 构造连接配置。
        /// 单机/主从/集群正常连接；若连接串或 ConfigurationOptions 中带 serviceName，
        /// 驱动会自动进入哨兵模式（连哨兵、发现主、故障转移自更新），无需手动设置 CommandMap.Sentinel。
        /// </summary>
        internal ConfigurationOptions GetConfiguredOptions()
        {
            var options = ConfigurationOptions != null
                ? ConfigurationOptions.Clone()
                : ConfigurationOptions.Parse(Configuration ?? string.Empty);

            if (DefaultDatabase >= 0)
                options.DefaultDatabase = DefaultDatabase;

            options.AbortOnConnectFail = false;
            return options;
        }

        internal RedisKey GetPrefixedKey(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return string.IsNullOrEmpty(InstancePrefix)
                ? (RedisKey)key
                : (RedisKey)(InstancePrefix + key);
        }
    }
}