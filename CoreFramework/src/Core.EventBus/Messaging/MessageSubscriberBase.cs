using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Messaging
{
    /// <summary>
    /// <see cref="IMessageSubscriber"/> 的通用基类:把扫程序集 → 展开 (messageType, handlerType) → 调 <see cref="SubscribeAsync(Type, Type, CancellationToken)"/> 的流程提取出来。
    /// </summary>
    public abstract class MessageSubscriberBase : IMessageSubscriber
    {
        /// <summary>扫描程序集中所有 handler 类型,按它实现的每个 <see cref="IMessageHandler{TMessage}"/> 展开成订阅对。</summary>
        public async Task InitializeAsync(Assembly[] assemblies, CancellationToken cancellationToken = default)
        {
            var handlerTypes = MessageHandlerExtensions.GetHandlerTypes(assemblies);
            foreach (var handlerType in handlerTypes)
            {
                var baseHandlerTypes = MessageHandlerExtensions.GetBaseHandlerTypes(handlerType);
                foreach (var baseHandlerType in baseHandlerTypes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var messageType = baseHandlerType.GenericTypeArguments[0];
                    await SubscribeAsync(messageType, handlerType, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>派生类实现:把一对 (messageType, handlerType) 注册到自己的订阅基础设施。</summary>
        protected abstract Task SubscribeAsync(Type messageType, Type handlerType, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract Task SubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
            where T : class, IMessage
            where TH : IMessageHandler<T>;

        /// <inheritdoc />
        public abstract Task UnSubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
            where T : class, IMessage
            where TH : IMessageHandler<T>;
    }
}
