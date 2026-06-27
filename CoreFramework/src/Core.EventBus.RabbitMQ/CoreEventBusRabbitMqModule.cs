using Core.Modularity;
using Core.Modularity.Attribute;
using Core.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    /// <para>
    /// <b>注册形态</b>:模块本身只把 broker 扩展挂到 <see cref="EventBusOptions.Extensions"/>。
    /// publisher / subscriber / IRabbitMqPublishChannelPool / IOutboxRawSender 的真正 DI 注册
    /// 由 <see cref="EventBusOptionsExtensions.AddServices"/> 在
    /// <see cref="CoreEventBusModule.PostConfigureServices"/> 阶段统一完成 —— 那时 <see cref="EventBusRabbitMqOptions"/>
    /// 也会真正绑到 <c>EventBus:RabbitMq</c> 配置节点。
    /// </para>
    /// <para>
    /// 旧版本在 ConfigureServices 里直接 <c>TryAddSingleton</c> publisher 等服务,**但从未
    /// `Configure&lt;EventBusRabbitMqOptions&gt;(...)`** —— 模块化用户的 <c>EventBus:RabbitMq:ExchangeName</c> /
    /// <c>FailureBehavior</c> / <c>DeadLetterExchange</c> / <c>ChannelPoolSize</c> 全部走默认值。本次修复对齐
    /// <see cref="Local.CoreEventBusLocalModule"/> 的做法,真正读取配置。
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
            var section = Configuration.GetSection("EventBus:RabbitMq");
            context.Services.Configure<EventBusOptions>(options =>
            {
                options.AddRabbitMq(section);
            });
        }
    }
}
