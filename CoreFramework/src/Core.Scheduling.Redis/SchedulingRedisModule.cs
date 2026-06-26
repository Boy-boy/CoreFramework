using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Scheduling.Redis
{
    /// <summary>
    /// Core.Scheduling 的 Redis 分布式锁模块,必须叠加在 <see cref="SchedulingBackgroundModule"/>(BG 宿主)之上:
    /// BG 提供主循环与 filter 管线;本模块用 Redis 锁替换 noop 锁,让多节点 BG 形成集群仲裁。
    /// 与 SchedulingQuartzModule / SchedulingHangfireModule 是平行的集群方案,三选一。
    /// 配置节:<c>Scheduling:Redis</c>。
    /// </summary>
    /// <remarks>适用:已在用 Redis 做缓存 / 会话,不想引入 Quartz 表或 Hangfire,只要"多节点同一时刻只一个跑"。</remarks>
    [DependsOn(typeof(SchedulingBackgroundModule))]
    public class SchedulingRedisModule : CoreModuleBase
    {
        public SchedulingRedisModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.Configure<RedisSchedulingOptions>(Configuration.GetSection("Scheduling:Redis"));

            var redisOptions = new RedisSchedulingOptions();
            Configuration.GetSection("Scheduling:Redis").Bind(redisOptions);

            context.Services.AddSchedulingRedisLock(opts => Copy(redisOptions, opts));
        }

        private static void Copy(RedisSchedulingOptions from, RedisSchedulingOptions to)
        {
            to.ConnectionString = from.ConnectionString;
            to.KeyPrefix = from.KeyPrefix;
            to.Database = from.Database;
        }
    }
}
