using System;
using System.Threading;
using Core.Configuration.Storage;
using Microsoft.Extensions.Configuration;

namespace Core.Configuration
{
    public abstract class DbConfigurationSource : IConfigurationSource
    {
        private readonly TimeSpan _minimumLifeTime = TimeSpan.FromSeconds(10.0);
        private TimeSpan _reloadDelay = TimeSpan.FromMinutes(1.0);

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DbConnection { get; set; }

        /// <summary>
        /// 若数据库不存在schema，即创建新的schema
        /// </summary>
        public string DbSchema { get; set; } = "core";

        /// <summary>
        /// 若数据库不存在table，即创建新的table
        /// </summary>
        public string DbTable { get; set; } = "sys_configuration";

        /// <summary>
        /// 配置信息环境（develop,test,product）
        /// </summary>
        public string Environment { get; set; } = Environments.Product;

        /// <summary>
        /// 配置信息组
        /// </summary>
        public string NameSpace { get; set; } = "all_server";

        /// <summary>
        /// 间隔多久同步table数据，默认1分钟一次,最低不可低于10秒
        /// </summary>
        public TimeSpan ReloadDelay
        {
            get => _reloadDelay;
            set
            {
                if (value != Timeout.InfiniteTimeSpan && value < _minimumLifeTime)
                    throw new ArgumentException(nameof(value));
                _reloadDelay = value;
            }
        }

        public abstract IConfigurationProvider Build(IConfigurationBuilder builder);
    }
}
