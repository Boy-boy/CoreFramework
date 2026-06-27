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
    /// <b>生命周期</b>:本类(及派生)注册为 Scoped。直接用注入的 <see cref="IServiceProvider"/>(即当前 scope 的 SP)解析 storage,
    /// 与 ambient UoW 所在 scope 一致,保证创建的 DbContext 由 UoW 拥有者 scope 持有,不会在 publish 返回后被提前释放。
    /// </para>
    /// </remarks>
    public abstract class IntegrationMessagePublisherBase : IMessagePublisher
    {
        private readonly IServiceProvider _serviceProvider;

        protected IntegrationMessagePublisherBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>统一入口;按 outbox 上下文是否存在自动选择路径。</summary>
        public virtual async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage
        {
            var ambient = _serviceProvider.GetService<IOutboxAmbientContext>();
            if (ambient?.IsActive == true)
            {
                // 从当前 scope 解析 storage:这里 _serviceProvider 是 ambient UoW 所在 scope 的 SP,
                // storage 内 IDbContextProvider 创建 / 复用的 DbContext 由该 scope 持有,生命周期与 UoW 对齐
                var storage = _serviceProvider.GetService<IOutboxStorage>();
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
