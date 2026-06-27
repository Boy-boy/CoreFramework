using Core.Kafka;
using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Kafka
{
    /// <summary>
    /// Kafka broker 模块。注册后业务侧 publisher/subscriber 可直接使用。
    /// </summary>
    /// <remarks>
    /// 配置节点:<c>EventBus:Kafka</c>。模块仅把 broker 扩展挂到 <see cref="EventBusOptions.Extensions"/>,
    /// publisher/subscriber/IOutboxRawSender 的真正 DI 注册由
    /// <see cref="EventBusOptionsExtensions.AddServices"/> 在 PostConfigureServices 阶段完成。
    /// </remarks>
    [DependsOn(typeof(CoreEventBusModule), typeof(CoreKafkaModule))]
    public class CoreEventBusKafkaModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreEventBusKafkaModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            var section = Configuration.GetSection("EventBus:Kafka");
            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AddKafka(section);
            });
        }
    }
}
