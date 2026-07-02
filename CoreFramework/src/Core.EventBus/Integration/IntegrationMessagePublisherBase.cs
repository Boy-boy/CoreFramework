using Core.EventBus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Integration
{
    /// <summary>集成事件 publisher 基类;按是否处于 outbox 上下文,选择写 outbox(事务一致)或直发 broker(best-effort)。broker 投递由派生类 <see cref="SendAsync{T}"/> 实现。</summary>
    /// <remarks>注册为 Scoped:注入的 <see cref="IServiceProvider"/> 是当前 scope 的 SP,与 ambient UoW 同 scope,storage 解析出的 DbContext 生命周期对齐 UoW。</remarks>
    public abstract class IntegrationMessagePublisherBase : IMessagePublisher
    {
        private readonly IServiceProvider _serviceProvider;

        protected IntegrationMessagePublisherBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>统一入口;按 outbox 上下文是否存在自动选择路径。</summary>
        /// <exception cref="ArgumentException"><paramref name="message"/>.Id 为 <see cref="Guid.Empty"/>(broker MessageId / inbox 去重键不可为空 Guid)。</exception>
        public virtual async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage
        {
            if (message.Id == Guid.Empty)
                throw new ArgumentException("IMessage.Id 不能为空 Guid;Message 基类构造已分配 Guid.CreateVersion7(),自定义实现请确保 Id 唯一。", nameof(message));

            var ambient = _serviceProvider.GetService<IOutboxAmbientContext>();
            if (ambient?.IsActive == true)
            {
                var storage = _serviceProvider.GetService<IOutboxStorage>();
                if (storage == null)
                {
                    throw new InvalidOperationException(
                        "检测到 outbox 上下文但未注册 IOutboxStorage。请在启动时注册 outbox 存储实现，" +
                        "例如 services.Configure<EventBusOptions>(o => o.AddEfCoreEventBusStorage<TDbContext>())。");
                }

                // 仅 Add 到 ChangeTracker;业务行 + outbox 行的同事务落库由外层 UoW.CommitAsync 完成
                await storage.StoreMessageAsync(new MessageEnvelope(message), cancellationToken);
                return;
            }

            // 无 UoW → 直发 broker
            await SendAsync(message, cancellationToken);
        }

        /// <summary>派生类实现:将强类型消息送达 broker;对应 dispatcher 侧的 <see cref="IOutboxRawSender.SendRawAsync"/>。</summary>
        public abstract Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage;
    }
}
