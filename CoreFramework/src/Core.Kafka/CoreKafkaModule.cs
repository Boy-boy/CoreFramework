using Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Kafka
{
    /// <summary>
    /// Kafka 基础设施模块;从 <c>appsettings.json</c> 的 <c>Kafka</c> 节点加载配置并注册
    /// <see cref="IKafkaPersistentProducer"/> / <see cref="IKafkaMessageConsumerManager"/> 等基础设施服务。
    /// </summary>
    public class CoreKafkaModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreKafkaModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.AddKafka(Configuration.GetSection("Kafka"));
        }
    }
}
