using Core.EventBus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Integration
{
    /// <summary>集成事件 publisher 基类;路由"写 outbox 还是直发 broker"。broker 投递由派生类 <see cref="SendAsync{T}"/> 实现。</summary>
    /// <remarks>
    /// <para><b>路由逻辑</b>:有 outbox 上下文 → <see cref="IOutboxStorage.StoreMessageAsync"/>(事务一致);无 → <see cref="SendAsync{T}"/>(best-effort)。</para>
    /// <para>
    /// <b>用 IServiceScopeFactory 的原因</b>:publisher 是 Singleton,直接持构造期 IServiceProvider 拿到的是根容器;
    /// 一旦 ambient context / storage 注册为 Scoped,scope-validation 开启时会抛"Cannot resolve scoped service from root provider"。
    /// 每次 PublishAsync 自建临时 scope 兼容任意生命周期。
    /// </para>
    /// </remarks>
    public abstract class IntegrationMessagePublisherBase : IMessagePublisher
    {
        private readonly IServiceScopeFactory _scopeFactory;

        protected IntegrationMessagePublisherBase(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        /// <summary>统一入口;按 outbox 上下文是否存在自动选择路径。</summary>
        public virtual async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage
        {
            // 临时 scope 解析,避免 Singleton publisher 拿根容器解析 Scoped 服务
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sp = scope.ServiceProvider;

            var ambient = sp.GetService<IOutboxAmbientContext>();
            if (ambient?.IsActive == true)
            {
                var storage = sp.GetService<IOutboxStorage>();
                if (storage == null)
                {
                    throw new InvalidOperationException(
                        "检测到 outbox 上下文但未注册 IOutboxStorage。请在启动时注册 outbox 存储实现，" +
                        "例如 services.Configure<EventBusOptions>(o => o.AddEfCoreEventBusStorage<TDbContext>())。");
                }

                // StoreMessageAsync 仅 Add 到 ChangeTracker,业务行 + outbox 行的同事务落库由外层 UoW.CommitAsync 完成
                await storage.StoreMessageAsync(new MessageEnvelope(message), cancellationToken);
                return;
            }

            // 无 UoW → 直发;能否送达取决于 broker 当时的可达性
            await SendAsync(message, cancellationToken);
        }

        /// <summary>派生类实现:将强类型消息送达 broker;对应 dispatcher 侧的 <see cref="IOutboxRawSender.SendRawAsync"/>。</summary>
        public abstract Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage;
    }
}
