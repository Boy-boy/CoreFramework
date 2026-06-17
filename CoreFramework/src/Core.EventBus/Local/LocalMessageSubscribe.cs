using Core.EventBus.Messaging;
using System;

namespace Core.EventBus.Local
{
    /// <summary>
    /// 本地事件订阅器实现。直接把订阅项写入 <see cref="ILocalMessageHandlerManager"/>，
    /// 不像 broker 实现那样需要声明 exchange / queue 之类的基础设施。
    /// </summary>
    public class LocalMessageSubscribe : MessageSubscribeBase, ILocalMessageSubscribe
    {
        private readonly ILocalMessageHandlerManager _messageHandlerManager;

        public LocalMessageSubscribe(ILocalMessageHandlerManager messageHandlerManager)
        {
            _messageHandlerManager = messageHandlerManager;
        }

        /// <inheritdoc />
        protected override void Subscribe(Type messageType, Type handlerType)
        {
            _messageHandlerManager.AddHandler(messageType, handlerType);
        }

        /// <inheritdoc />
        public override void Subscribe<T, TH>()
        {
            Subscribe(typeof(T), typeof(TH));
        }

        /// <inheritdoc />
        public override void UnSubscribe<T, TH>()
        {
            _messageHandlerManager.RemoveHandler(typeof(T), typeof(TH));
        }
    }
}
