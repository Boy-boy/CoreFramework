using Core.EventBus.Inbox;
using Core.EventBus.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>
    /// EF Core 版 outbox / inbox 存储的 <see cref="IEventBusOptionsExtensions"/> 实现:绑定 options、
    /// 注册 storage、inbox-aware invoker 与后台调度服务。publisher / <see cref="IOutboxRawSender"/> 由 broker 模块负责。
    /// </summary>
    /// <remarks>
    /// 支持两种配置来源(构造二选一):代码侧 Action,或 <see cref="IConfiguration"/> 节点。
    /// <see cref="AddServices"/> 按设置的那个执行绑定。
    /// </remarks>
    public class EventBusOptionsExtensions<TDbContext> : IEventBusOptionsExtensions
        where TDbContext : DbContext
    {
        private readonly Action<OutboxOptions> _configureOutbox;
        private readonly Action<InboxOptions> _configureInbox;
        private readonly IConfiguration _outboxConfiguration;
        private readonly IConfiguration _inboxConfiguration;

        public EventBusOptionsExtensions(
            Action<OutboxOptions> configureOutbox = null,
            Action<InboxOptions> configureInbox = null)
        {
            _configureOutbox = configureOutbox;
            _configureInbox = configureInbox;
        }

        public EventBusOptionsExtensions(
            IConfiguration outboxConfiguration,
            IConfiguration inboxConfiguration = null)
        {
            _outboxConfiguration = outboxConfiguration ?? throw new ArgumentNullException(nameof(outboxConfiguration));
            _inboxConfiguration = inboxConfiguration;
        }

        /// <summary>绑定 options 并注册 storage / inbox-aware invoker / 后台服务;由 PostConfigureServices 阶段调用。</summary>
        public void AddServices(IServiceCollection services)
        {
            services.AddOptions<OutboxOptions>();
            if (_configureOutbox != null)
            {
                services.Configure(_configureOutbox);
            }
            else if (_outboxConfiguration != null)
            {
                services.Configure<OutboxOptions>(_outboxConfiguration);
            }

            services.AddOptions<InboxOptions>();
            if (_configureInbox != null)
            {
                services.Configure(_configureInbox);
            }
            else if (_inboxConfiguration != null)
            {
                services.Configure<InboxOptions>(_inboxConfiguration);
            }

            // Scoped:每个 DI scope 持有自己的 storage 实例,通过 IDbContextProvider 拿到当前 scope 的 DbContext,
            // 保证生产者写入和业务在同一上下文
            services.TryAddScoped<IOutboxStorage, EfCoreOutboxStorage<TDbContext>>();
            services.TryAddScoped<IInboxStorage, EfCoreInboxStorage<TDbContext>>();

            // 只替换 DefaultMessageHandlerInvoker 这一具体实现,保留用户自定义 wrapper / decorator;
            // 同时 TryAddSingleton 防止本扩展被多次调用产生多个 inbox-aware invoker 实例
            for (var i = services.Count - 1; i >= 0; i--)
            {
                var sd = services[i];
                if (sd.ServiceType == typeof(IMessageHandlerInvoker)
                    && sd.ImplementationType == typeof(DefaultMessageHandlerInvoker))
                {
                    services.RemoveAt(i);
                }
            }
            services.TryAddSingleton<IMessageHandlerInvoker, InboxAwareMessageHandlerInvoker>();

            // 防重复注册:AddHostedService 无 Try 语义,本扩展被多次调用(模块多次注入 / 多 DbContext)
            // 会启动多个 dispatcher 实例 → 与 FetchReadyAsync 无锁配合产生 N 倍重复投递。
            // 多 DbContext 场景下 IOutboxStorage 只能注册一个,多 dispatcher 也无意义。
            if (!services.Any(s => s.ImplementationType == typeof(OutboxDispatcher)))
            {
                services.AddHostedService<OutboxDispatcher>();
            }
            if (!services.Any(s => s.ImplementationType == typeof(InboxCleanupService)))
            {
                services.AddHostedService<InboxCleanupService>();
            }
        }
    }
}
