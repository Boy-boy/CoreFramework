using System;

namespace Core.EventBus
{
    /// <summary>
    /// <see cref="IMessageHandlerWrapper"/> 的强类型实现;由 <see cref="MessageHandlerManager.AddHandler"/> 反射 <c>MakeGenericType</c> 实例化。
    /// </summary>
    /// <typeparam name="TMessage">本 wrapper 描述的消息类型。</typeparam>
    /// <remarks>
    /// 只承载元数据;handler 实例永远不能缓存在这里,必须由 <see cref="IMessageHandlerInvoker"/> 按当前 scope 重新 resolve。
    /// </remarks>
    public class MessageHandlerWrapper<TMessage> : IMessageHandlerWrapper
        where TMessage : class, IMessage
    {
        public MessageHandlerWrapper(Type handlerType)
        {
            MessageName = MessageNameAttribute.GetNameOrDefault(typeof(TMessage));
            MessageType = typeof(TMessage);
            HandlerType = handlerType;
            HandlerPriority = MessageHandlerPriorityAttribute.GetPriority(typeof(TMessage), handlerType);
        }

        /// <inheritdoc />
        public string MessageName { get; }

        /// <inheritdoc />
        public Type MessageType { get; }

        /// <inheritdoc />
        public Type HandlerType { get; }

        /// <inheritdoc />
        public int HandlerPriority { get; }
    }
}
