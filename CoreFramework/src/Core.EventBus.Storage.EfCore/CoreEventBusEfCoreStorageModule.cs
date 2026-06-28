using Core.EntityFrameworkCore;
using Core.Modularity;
using Core.Modularity.Attribute;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>EF Core 版 outbox / inbox 存储模块;消费端自动获得 UoW + inbox 去重。</summary>
    /// <remarks>
    /// <para>依赖:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="CoreEventBusModule"/>:EventBus 抽象</description></item>
    ///   <item><description><see cref="CoreEfCoreModule"/>:EF Core 仓储 / DbContext</description></item>
    /// </list>
    /// <para>
    /// storage 是泛型的(<c>TDbContext</c> 未知),模块无法在 ConfigureServices 阶段注册;
    /// 业务侧需显式调 <c>options.AddEfCoreEventBusStorage&lt;TDbContext&gt;()</c>,由扩展在
    /// <see cref="CoreEventBusModule.PostConfigureServices"/> 阶段注册 storage / invoker / 后台服务。
    /// </para>
    /// <para>模块本身只承担 DependsOn 编排,无需注册任何服务也无需读取 <c>IConfiguration</c>。</para>
    /// </remarks>
    [DependsOn(typeof(CoreEventBusModule),
        typeof(CoreEfCoreModule))]
    public class CoreEventBusEfCoreStorageModule : CoreModuleBase
    {
        public override void ConfigureServices(ServiceCollectionContext context)
        {
            // 真正的服务注册在 EventBusOptionsExtensions<TDbContext>.AddServices 阶段完成
        }
    }
}
