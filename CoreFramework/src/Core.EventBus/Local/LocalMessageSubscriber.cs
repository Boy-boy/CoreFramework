using Core.EventBus.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Local
{
    /// <summary>
    /// 本地事件订阅器实现。直接把订阅项写入 <see cref="ILocalMessageHandlerManager"/>，
    /// 不像 broker 实现那样需要声明 exchange / queue 之类的基础设施。
    /// </summary>
    public class LocalMessageSubscriber : MessageSubscriberBase, ILocalSubscriber
    {
        private readonly ILocalMessageHandlerManager _messageHandlerManager;

        public LocalMessageSubscriber(ILocalMessageHandlerManager messageHandlerManager)
        {
            _messageHandlerManager = messageHandlerManager;
        }

        /// <inheritdoc />
        protected override Task SubscribeAsync(Type messageType, Type handlerType, CancellationToken cancellationToken)
        {
            _messageHandlerManager.AddHandler(messageType, handlerType);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task SubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(typeof(T), typeof(TH), cancellationToken);
        }

        /// <inheritdoc />
        public override Task UnSubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
        {
            _messageHandlerManager.RemoveHandler(typeof(T), typeof(TH));
            return Task.CompletedTask;
        }
    }
}
