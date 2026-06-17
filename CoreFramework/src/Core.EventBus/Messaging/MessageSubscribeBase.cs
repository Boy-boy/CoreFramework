using System;
using System.Reflection;

namespace Core.EventBus.Messaging
{
    /// <summary>
    /// <see cref="IMessageSubscribe"/> 的通用基类：把"扫程序集 → 反射出 (messageType, handlerType) →
    /// 调 <see cref="Subscribe(Type, Type)"/>"的共通流程提取出来，各实现只需要关心
    /// "把一对 (messageType, handlerType) 注册到自己的订阅基础设施"。
    /// </summary>
    public abstract class MessageSubscribeBase : IMessageSubscribe
    {
        /// <summary>
        /// 扫描每个程序集中所有 <see cref="IMessageHandler"/> 实现类型，按它实现的所有
        /// <see cref="IMessageHandler{TMessage}"/> 泛型接口逐一展开成 (messageType, handlerType)，
        /// 逐对调 <see cref="Subscribe(Type, Type)"/> 完成订阅。
        /// </summary>
        public void Initialize(params Assembly[] assemblies)
        {
            var handlerTypes = MessageHandlerExtensions.GetHandlerTypes(assemblies);
            foreach (var handlerType in handlerTypes)
            {
                var baseHandlerTypes = MessageHandlerExtensions.GetBaseHandlerTypes(handlerType);
                foreach (var baseHandlerType in baseHandlerTypes)
                {
                    var messageType = baseHandlerType.GenericTypeArguments[0];
                    Subscribe(messageType, handlerType);
                }
            }
        }

        /// <summary>
        /// 派生类实现：把一对 (messageType, handlerType) 注册到自己的订阅基础设施。
        /// </summary>
        protected abstract void Subscribe(Type messageType, Type handlerType);

        /// <inheritdoc />
        public abstract void Subscribe<T, TH>()
            where T : class, IMessage
            where TH : IMessageHandler<T>;

        /// <inheritdoc />
        public abstract void UnSubscribe<T, TH>()
            where T : class, IMessage
            where TH : IMessageHandler<T>;
    }
}
