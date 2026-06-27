using Core.EventBus.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Local
{
    /// <summary>本地事件订阅器实现;订阅项直接写入 <see cref="ILocalMessageHandlerManager"/>。</summary>
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
