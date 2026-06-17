using Core.EventBus;
using Core.EventBus.Inbox;
using Core.EventBus.Outbox;
using Core.EventBus.Storage.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 提供 <c>options.AddEfCoreEventBusStorage&lt;TDbContext&gt;(...)</c> 流式扩展，
    /// 把 EF Core 版 outbox / inbox 存储加入 EventBus 扩展链。支持 Action 配置与 IConfiguration 两种来源。
    /// </summary>
    /// <remarks>
    /// <para><b>调用本扩展会一次性完成（在 <c>PostConfigureServices</c> 阶段由
    /// <see cref="Core.EventBus.Storage.EfCore.EventBusOptionsExtensions{TDbContext}.AddServices"/> 应用）：</b></para>
    /// <list type="bullet">
    ///   <item><description>注册 <see cref="IOutboxStorage"/> = <see cref="EfCoreOutboxStorage{TDbContext}"/>（Scoped）</description></item>
    ///   <item><description>注册 <see cref="IInboxStorage"/> = <see cref="EfCoreInboxStorage{TDbContext}"/>（Scoped）</description></item>
    ///   <item><description>注册 <see cref="StorageMarkerService"/> 标识"项目已启用存储"</description></item>
    ///   <item><description><b>替换</b> <see cref="IMessageHandlerInvoker"/> 为 <see cref="InboxAwareMessageHandlerInvoker"/>
    ///   —— 消费端自动获得 UoW + inbox 去重</description></item>
    ///   <item><description>启动 <see cref="OutboxDispatcher"/> 后台投递服务</description></item>
    ///   <item><description>启动 <see cref="InboxCleanupService"/> 后台清理服务</description></item>
    /// </list>
    ///
    /// <para><b>前置条件</b></para>
    /// <list type="bullet">
    ///   <item><description>已通过 <c>services.AddUnitOfWork()</c> 注册 UoW（典型由 <c>CoreUnitOfWorkModule</c> 自动完成）</description></item>
    ///   <item><description>已注册业务 <typeparamref name="TDbContext"/>（典型由 <c>AddDbContextAndEfRepositories&lt;TDbContext&gt;</c>）</description></item>
    ///   <item><description>业务 DbContext.OnModelCreating 已调用 <c>modelBuilder.AddEventBusStorage()</c></description></item>
    ///   <item><description>broker 模块（如 <c>CoreEventBusRabbitMqModule</c>）已注册 <see cref="IOutboxRawSender"/></description></item>
    /// </list>
    ///
    /// <para><b>典型用法</b></para>
    /// <code>
    /// // 模块化场景：通过 Configure&lt;EventBusOptions&gt; 把扩展挂到 options
    /// services.Configure&lt;EventBusOptions&gt;(options =&gt;
    /// {
    ///     options.AddEfCoreEventBusStorage&lt;CustomerDbContext&gt;();
    /// });
    ///
    /// // 非模块化场景：直接在 AddEventBus 回调中链式调用
    /// services.AddEventBus(options =&gt;
    /// {
    ///     options.AddRabbitMq(Configuration.GetSection("EventBus:RabbitMq"));
    ///     options.AddEfCoreEventBusStorage&lt;CustomerDbContext&gt;(
    ///         configureOutbox: o =&gt; { o.BatchSize = 200; o.MaxRetries = 5; },
    ///         configureInbox:  i =&gt; { i.RetentionDays = 30; });
    /// });
    /// </code>
    /// </remarks>
    public static class EfCoreStorageServiceCollectionExtensions
    {
        /// <summary>
        /// 用 <c>Action&lt;OutboxOptions&gt;</c> / <c>Action&lt;InboxOptions&gt;</c> 形式配置 outbox / inbox。
        /// 适合代码侧组装 / 测试场景。两个回调均为可选 —— 不传即用默认值。
        /// </summary>
        public static EventBusOptions AddEfCoreEventBusStorage<TDbContext>(
            this EventBusOptions options,
            Action<OutboxOptions> configureOutbox = null,
            Action<InboxOptions> configureInbox = null)
            where TDbContext : DbContext
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            options.AddExtensions(new EventBusOptionsExtensions<TDbContext>(configureOutbox, configureInbox));
            return options;
        }

        /// <summary>
        /// 用 appsettings.json 节点配置 outbox / inbox。<paramref name="outboxSection"/> 通常是
        /// <c>Configuration.GetSection("EventBus:Outbox")</c>；<paramref name="inboxSection"/> 可选。
        /// </summary>
        public static EventBusOptions AddEfCoreEventBusStorage<TDbContext>(
            this EventBusOptions options,
            IConfiguration outboxSection,
            IConfiguration inboxSection = null)
            where TDbContext : DbContext
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (outboxSection == null)
                throw new ArgumentNullException(nameof(outboxSection));

            options.AddExtensions(new EventBusOptionsExtensions<TDbContext>(outboxSection, inboxSection));
            return options;
        }
    }
}
