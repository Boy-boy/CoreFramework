using Core.Modularity;
using Core.Modularity.Attribute;
using Core.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>RabbitMQ broker 模块;注册后业务侧的 publisher/subscriber 即可直接使用。</summary>
    /// <remarks>
    /// 仅把 broker 扩展挂到 <see cref="EventBusOptions.Extensions"/>;publisher / subscriber /
    /// IRabbitMqPublishChannelPool / IOutboxRawSender 的真正 DI 注册由
    /// <see cref="EventBusOptionsExtensions.AddServices"/> 在
    /// <see cref="CoreEventBusModule.PostConfigureServices"/> 阶段统一完成。配置来源为 <c>EventBus:RabbitMq</c> 节点。
    /// </remarks>
    [DependsOn(typeof(CoreEventBusModule),
        typeof(CoreRabbitMqModule))]
    public class CoreEventBusRabbitMqModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreEventBusRabbitMqModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            var section = Configuration.GetSection("EventBus:RabbitMq");
            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AddRabbitMq(section);
            });
        }
    }
}
