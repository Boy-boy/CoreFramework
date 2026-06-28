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
    /// <c>options.AddEfCoreEventBusStorage&lt;TDbContext&gt;()</c> 流式扩展,
    /// 把 EF Core 版 outbox / inbox 存储加入 EventBus 扩展链。
    /// </summary>
    /// <remarks>
    /// <para>调用本扩展(在 PostConfigureServices 阶段由 <see cref="Core.EventBus.Storage.EfCore.EventBusOptionsExtensions{TDbContext}.AddServices"/> 应用)会:</para>
    /// <list type="bullet">
    ///   <item><description>注册 <see cref="IOutboxStorage"/> / <see cref="IInboxStorage"/>(Scoped)</description></item>
    ///   <item><description>替换 <see cref="IMessageHandlerInvoker"/> 为 <see cref="InboxAwareMessageHandlerInvoker"/> —— 消费端自动 UoW + 去重</description></item>
    ///   <item><description>启动 <see cref="OutboxDispatcher"/> / <see cref="InboxCleanupService"/> 后台服务</description></item>
    /// </list>
    /// <para>前置条件:</para>
    /// <list type="bullet">
    ///   <item><description>已注册 UoW 与业务 <typeparamref name="TDbContext"/></description></item>
    ///   <item><description>业务 DbContext.OnModelCreating 已调 <c>modelBuilder.AddEventBusStorage()</c></description></item>
    ///   <item><description>broker 模块已注册 <see cref="IOutboxRawSender"/></description></item>
    /// </list>
    /// </remarks>
    public static class EfCoreStorageServiceCollectionExtensions
    {
        /// <summary>用 Action 形式配置 outbox / inbox / 死信清理(均可选,不传即默认)。</summary>
        public static EventBusOptions AddEfCoreEventBusStorage<TDbContext>(
            this EventBusOptions options,
            Action<OutboxOptions> configureOutbox = null,
            Action<InboxOptions> configureInbox = null,
            Action<DeadLetterCleanupOptions> configureDeadLetter = null)
            where TDbContext : DbContext
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            options.AddExtensions(new EventBusOptionsExtensions<TDbContext>(configureOutbox, configureInbox, configureDeadLetter));
            return options;
        }

        /// <summary>用 appsettings.json 节点配置 outbox / inbox / 死信清理;后两个节点可选。</summary>
        public static EventBusOptions AddEfCoreEventBusStorage<TDbContext>(
            this EventBusOptions options,
            IConfiguration outboxSection,
            IConfiguration inboxSection = null,
            IConfiguration deadLetterSection = null)
            where TDbContext : DbContext
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (outboxSection == null)
                throw new ArgumentNullException(nameof(outboxSection));

            options.AddExtensions(new EventBusOptionsExtensions<TDbContext>(outboxSection, inboxSection, deadLetterSection));
            return options;
        }
    }
}
