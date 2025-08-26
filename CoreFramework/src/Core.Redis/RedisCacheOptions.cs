using StackExchange.Redis;

namespace Core.Redis
{
    public class RedisCacheOptions
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string Configuration { get; set; }

        public ConfigurationOptions ConfigurationOptions { get; set; }

        /// <summary>
        /// 键前缀
        /// </summary>
        public string InstancePrefix { get; set; }

        /// <summary>
        /// 连接健康检查
        /// 默认60秒检测一次
        /// </summary>
        public int ConnectionHealthCheck { get; set; } = 60;

        internal ConfigurationOptions GetConfiguredOptions()
        {
            var options = ConfigurationOptions ?? ConfigurationOptions.Parse(Configuration!);
            options.AbortOnConnectFail = false;

            //options.ConnectRetry = 3; // 重试次数
            //options.ConnectTimeout = 10000; // 连接超时时间
            //options.SyncTimeout = 10000; // 同步操作超时时间
            //options.KeepAlive = 10; // 保持连接活跃

            return options;
        }

        /// <summary>
        /// 为键添加实例前缀
        /// </summary>
        /// <param name="key">原始键名</param>
        /// <returns>带前缀的RedisKey</returns>
        /// <exception cref="ArgumentNullException">当key为null时抛出</exception>
        internal RedisKey GetPrefixedKey(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return string.IsNullOrEmpty(InstancePrefix)
                ? key
                : InstancePrefix + key;
        }
    }
}
