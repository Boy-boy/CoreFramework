using Core.EventBus.Inbox;
using Core.EventBus.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>
    /// EF Core 版 outbox / inbox 存储的 <see cref="IEventBusOptionsExtensions"/> 实现：
    /// 仅负责把 <see cref="OutboxOptions"/> / <see cref="InboxOptions"/> 绑到 IOptions，
    /// 以及把 storage、inbox-aware invoker 与后台调度服务挂到 IoC。
    /// publisher / subscribe / <see cref="IOutboxRawSender"/> 等服务由 broker 模块（如 <c>CoreEventBusRabbitMqModule</c>）注册。
    /// </summary>
    /// <remarks>
    /// 支持两种配置来源：
    /// <list type="bullet">
    ///   <item><description>代码侧 <c>Action&lt;OutboxOptions&gt;</c> / <c>Action&lt;InboxOptions&gt;</c>：用于测试 / 程序内组装。</description></item>
    ///   <item><description><see cref="IConfiguration"/> 节点：典型用于 appsettings.json 驱动。</description></item>
    /// </list>
    /// 两者互斥（构造函数二选一），<see cref="AddServices"/> 中按设置的那个执行绑定。
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

        /// <summary>
        /// 把 outbox / inbox 配置绑到 IOptions，并注册 storage、inbox-aware invoker 与后台服务。
        /// 由 <see cref="EventBusOptionsExtensions.Configure"/> 在 PostConfigureServices 阶段调用。
        /// </summary>
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

            // outbox / inbox 是 Scoped：每个 DI scope 持有自己的 storage 实例，
            // 通过 IDbContextProvider 拿到当前 scope 的 DbContext —— 保证生产者写入和业务在同一上下文
            services.TryAddScoped<IOutboxStorage, EfCoreOutboxStorage<TDbContext>>();
            services.TryAddScoped<IInboxStorage, EfCoreInboxStorage<TDbContext>>();
            services.TryAddSingleton<StorageMarkerService>();

            // 关键：先 RemoveAll 再 AddSingleton 强制替换 AddEventBus 注册的 DefaultMessageHandlerInvoker。
            // 这样消费端无需任何业务侧改动就自动获得 UoW + 幂等去重
            services.RemoveAll<IMessageHandlerInvoker>();
            services.AddSingleton<IMessageHandlerInvoker, InboxAwareMessageHandlerInvoker>();

            services.AddHostedService<OutboxDispatcher>();
            services.AddHostedService<InboxCleanupService>();
        }
    }
}
