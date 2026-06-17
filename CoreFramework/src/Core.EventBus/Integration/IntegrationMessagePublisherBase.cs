using Core.EventBus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Integration
{
    /// <summary>
    /// 集成事件 publisher 的基类，承担"业务调用 PublishAsync 时，是写 outbox 还是直发 broker"的路由判断。
    /// 具体的 broker 投递由派生类实现 <see cref="SendAsync{T}"/>（如 <c>RabbitMqMessagePublisher</c>）。
    /// </summary>
    /// <remarks>
    /// <para><b>判定逻辑（PublishAsync）</b></para>
    /// <list type="number">
    ///   <item><description>查 <see cref="IOutboxAmbientContext"/>：当前有 outbox 上下文（典型为活跃 UoW）？</description></item>
    ///   <item><description>有 → 调 <see cref="IOutboxStorage.StoreMessageAsync"/> 把消息放进 outbox（事务一致性路径）</description></item>
    ///   <item><description>无 → 调 <see cref="SendAsync{T}"/> 直发 broker（best-effort，无业务一致性保证）</description></item>
    /// </list>
    ///
    /// <para><b>为什么用 IServiceScopeFactory 而非直接持 IServiceProvider</b></para>
    /// <para>
    /// publisher 自身注册为 Singleton；如果直接保留构造时注入的 IServiceProvider，那拿到的是
    /// <b>根容器</b>。一旦 <see cref="IOutboxAmbientContext"/> / <see cref="IOutboxStorage"/> 被注册成 Scoped
    /// （这是非常自然的选择），在开启 scope-validation 的环境下会立即抛
    /// "Cannot resolve scoped service from root provider"。
    /// 改用 <see cref="IServiceScopeFactory"/> 后，每次 PublishAsync 在自己的临时 scope 里 resolve，
    /// 不论 ambient context / storage 注册成什么生命周期都能正常工作。
    /// </para>
    /// </remarks>
    public abstract class IntegrationMessagePublisherBase : IMessagePublisher
    {
        private readonly IServiceScopeFactory _scopeFactory;

        protected IntegrationMessagePublisherBase(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        /// <summary>
        /// 业务侧统一入口：根据当前是否处于 outbox 上下文，自动选择 outbox 或直发路径。
        /// </summary>
        public virtual async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage
        {
            // 在临时 scope 内 resolve ambient context 和 storage，避免 Singleton publisher 持根容器
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

                // 注意：StoreMessageAsync 不会直接 SaveChanges/COMMIT，只是把行加入 ChangeTracker。
                // 真正的"业务行 + outbox 行同事务落库"由外层 UoW.CommitAsync 完成
                await storage.StoreMessageAsync(new MessageEnvelope(message), cancellationToken);
                return;
            }

            // 无 UoW → 直发。事件能否送达取决于 broker 当时的可达性
            await SendAsync(message, cancellationToken);
        }

        /// <summary>
        /// 派生类实现：将消息真正送达 broker（如 RabbitMQ）。
        /// 该方法是 outbox dispatcher <see cref="IOutboxRawSender.SendRawAsync"/> 的"业务侧入口"对应物，
        /// 但接收强类型消息而非 <see cref="MessageEnvelope"/>。
        /// </summary>
        public abstract Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage;
    }
}
