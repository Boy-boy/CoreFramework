using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Messaging
{
    /// <summary>
    /// <see cref="IMessageSubscriber"/> 的通用基类：把"扫程序集 → 反射出 (messageType, handlerType) →
    /// 调 <see cref="SubscribeAsync(Type, Type, CancellationToken)"/>"的共通流程提取出来，各实现只需要关心
    /// "把一对 (messageType, handlerType) 注册到自己的订阅基础设施"。
    /// </summary>
    public abstract class MessageSubscriberBase : IMessageSubscriber
    {
        /// <summary>
        /// 扫描每个程序集中所有 <see cref="IMessageHandler"/> 实现类型，按它实现的所有
        /// <see cref="IMessageHandler{TMessage}"/> 泛型接口逐一展开成 (messageType, handlerType)，
        /// 逐对调 <see cref="SubscribeAsync(Type, Type, CancellationToken)"/> 完成订阅。
        /// </summary>
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

        /// <summary>
        /// 派生类实现：把一对 (messageType, handlerType) 注册到自己的订阅基础设施。
        /// </summary>
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
