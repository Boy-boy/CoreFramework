using Core.EntityFrameworkCore;
using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>
    /// EF Core 版 outbox / inbox 存储模块。注册成功后，由 broker 模块（如 <c>CoreEventBusRabbitMqModule</c>）
    /// 投递的消息将通过 outbox 持久化并发送，消费端自动获得 UoW + inbox 去重。
    /// </summary>
    /// <remarks>
    /// <para>依赖：</para>
    /// <list type="bullet">
    ///   <item><description><see cref="CoreEventBusModule"/>：EventBus 抽象与 DefaultMessageHandlerInvoker</description></item>
    ///   <item><description><see cref="CoreEfCoreModule"/>：EF Core 仓储 / DbContext 注册（间接依赖 <c>CoreUnitOfWorkModule</c>）</description></item>
    /// </list>
    ///
    /// <para>
    /// 配置来源：<c>appsettings.json</c> 的 <c>EventBus:Outbox</c> / <c>EventBus:Inbox</c> 节点。
    /// </para>
    ///
    /// <para><b>注意</b></para>
    /// <para>
    /// 由于 outbox / inbox storage 是泛型实现（<see cref="EfCoreOutboxStorage{TDbContext}"/> /
    /// <see cref="EfCoreInboxStorage{TDbContext}"/>），本模块无法在 <see cref="ConfigureServices"/>
    /// 阶段确定 <c>TDbContext</c>。业务侧仍需在 <c>AddEventBus</c> 回调或
    /// <c>services.Configure&lt;EventBusOptions&gt;</c> 中显式调用
    /// <c>options.AddEfCoreEventBusStorage&lt;TDbContext&gt;(...)</c> 把扩展挂入扩展链，
    /// 真正的 storage / invoker / 后台服务由该扩展在
    /// <see cref="CoreEventBusModule.PostConfigureServices"/> 阶段统一注册。
    /// </para>
    /// </remarks>
    [DependsOn(typeof(CoreEventBusModule),
        typeof(CoreEfCoreModule))]
    public class CoreEventBusEfCoreStorageModule : CoreModuleBase
    {
        public IConfiguration Configuration { get; }

        public CoreEventBusEfCoreStorageModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void ConfigureServices(ServiceCollectionContext context)
        {
            context.Services.TryAddSingleton<StorageMarkerService>();
        }
    }
}
