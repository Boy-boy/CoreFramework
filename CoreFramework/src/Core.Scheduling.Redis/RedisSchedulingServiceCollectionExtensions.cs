using System;
using Core.Scheduling.DistributedLocking;
using Core.Scheduling.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using global::StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Core.Scheduling.Redis 依赖注入扩展。</summary>
    public static class RedisSchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 用 Redis 分布式锁替换默认 noop 实现,叠加在 <c>AddSchedulingBackground</c> 之上构成轻量集群方案。
        /// </summary>
        /// <remarks>
        /// <see cref="IConnectionMultiplexer"/> 由调用方提供:
        /// <list type="bullet">
        /// <item>DI 中已有(<c>Core.Redis</c> 或自行 <c>services.AddSingleton(...)</c>),本扩展直接复用;</item>
        /// <item>否则在 <see cref="RedisSchedulingOptions.ConnectionString"/> 中给连接串,本扩展建一个单例。</item>
        /// </list>
        /// <paramref name="configureOptions"/> 注册期同步调用一次,回调里的 I/O 或日志不会双触发。
        /// </remarks>
        public static IServiceCollection AddSchedulingRedisLock(
            this IServiceCollection services,
            Action<RedisSchedulingOptions> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            // 回调只跑一次,避免有副作用(读 env、写日志、订阅事件)的注册函数被双触发
            var snapshot = new RedisSchedulingOptions();
            configureOptions(snapshot);

            services.Configure<RedisSchedulingOptions>(o =>
            {
                o.ConnectionString = snapshot.ConnectionString;
                o.KeyPrefix = snapshot.KeyPrefix;
                o.Database = snapshot.Database;
            });

            // 未注册 IConnectionMultiplexer 且 options 给了连接串,则代建一个单例
            if (!string.IsNullOrWhiteSpace(snapshot.ConnectionString))
            {
                services.TryAddSingleton<IConnectionMultiplexer>(_ =>
                    ConnectionMultiplexer.Connect(snapshot.ConnectionString));
            }

            // Replace 强制覆盖默认 noop(无论它是否已注册)
            services.Replace(ServiceDescriptor.Singleton<IDistributedHandlerLock, RedisDistributedHandlerLock>());

            return services;
        }
    }
}
