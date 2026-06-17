using Core.EventBus.Integration;
using Core.EventBus.Outbox;
using Core.Modularity;
using Core.Modularity.Attribute;
using Core.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.RabbitMQ
{
    /// <summary>
    /// RabbitMQ broker 模块。注册成功后业务侧的 publisher/subscriber 即可直接用。
    /// </summary>
    /// <remarks>
    /// <para>依赖：</para>
    /// <list type="bullet">
    ///   <item><description><see cref="CoreEventBusModule"/>：EventBus 抽象与 DefaultMessageHandlerInvoker</description></item>
    ///   <item><description><c>CoreRabbitMqModule</c>：RabbitMQ 连接管理（IRabbitMqPersistentConnection 等）</description></item>
    /// </list>
    /// <para>
    /// 配置来源：<c>appsettings.json</c> 的 <c>EventBus:RabbitMq</c> 节点。
    /// </para>
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
            context.Services.TryAddSingleton<IIntegrationMessagePublisher, RabbitMqMessagePublisher>();
            context.Services.TryAddSingleton<IIntegrationMessageSubscribe, RabbitMqMessageSubscribe>();
            context.Services.TryAddSingleton(sp =>
                (IOutboxRawSender)sp.GetRequiredService<IIntegrationMessagePublisher>());
            context.Services.AddIntegrationCore();
        }
    }
}
