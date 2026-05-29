using StackExchange.Redis;

namespace Core.Redis
{
    public class RedisCacheOptions
    {
        /// <summary>
        /// 连接字符串
        /// 单机/主从/集群：host1:6379,host2:6379,...
        /// 哨兵：填写哨兵节点列表 host1:26379,host2:26379,host3:26379
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// 直接提供 StackExchange.Redis 的 ConfigurationOptions，若设置则优先生效。
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
        /// 访问密码（Sentinel 模式下用于数据节点）
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 是否启用 Sentinel 哨兵模式
        /// </summary>
        public bool UseSentinel { get; set; }

        /// <summary>
        /// 哨兵监控的主节点名，UseSentinel=true 时必填
        /// </summary>
        public string SentinelServiceName { get; set; }

        /// <summary>
        /// 哨兵自身的访问密码（与数据节点 Password 区分）
        /// </summary>
        public string SentinelPassword { get; set; }

        internal ConfigurationOptions GetConfiguredOptions()
        {
            ConfigurationOptions options;
            if (ConfigurationOptions != null)
            {
                options = ConfigurationOptions.Clone();
            }
            else
            {
                options = ConfigurationOptions.Parse(Configuration ?? string.Empty);
                ApplySimpleDefaults(options);
            }

            options.AbortOnConnectFail = false;
            return options;
        }

        private void ApplySimpleDefaults(ConfigurationOptions options)
        {
            if (DefaultDatabase >= 0)
                options.DefaultDatabase = DefaultDatabase;

            if (!string.IsNullOrEmpty(Password) && string.IsNullOrEmpty(options.Password))
                options.Password = Password;
        }

        internal ConfigurationOptions GetSentinelOptions()
        {
            ConfigurationOptions options;
            if (ConfigurationOptions != null)
            {
                options = ConfigurationOptions.Clone();
                if (!string.IsNullOrEmpty(SentinelPassword))
                    options.Password = SentinelPassword;
            }
            else
            {
                options = ConfigurationOptions.Parse(Configuration ?? string.Empty);
                if (!string.IsNullOrEmpty(SentinelPassword))
                    options.Password = SentinelPassword;
            }

            options.AbortOnConnectFail = false;
            options.TieBreaker = string.Empty;
            options.CommandMap = CommandMap.Sentinel;
            options.ServiceName = SentinelServiceName;

            return options;
        }

        internal ConfigurationOptions GetSentinelDataOptions()
        {
            ConfigurationOptions options;
            if (ConfigurationOptions != null)
            {
                options = ConfigurationOptions.Clone();
                if (!string.IsNullOrEmpty(Password))
                    options.Password = Password;
            }
            else
            {
                options = ConfigurationOptions.Parse(Configuration ?? string.Empty);

                if (DefaultDatabase >= 0)
                    options.DefaultDatabase = DefaultDatabase;

                if (!string.IsNullOrEmpty(Password))
                    options.Password = Password;
            }

            options.ServiceName = SentinelServiceName;
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