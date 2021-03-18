using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.EventBus
{
    public class MessageHandlerProvider : IMessageHandlerProvider
    {
        private readonly IMessageHandlerManager _messageHandlerManager;

        public MessageHandlerProvider(IMessageHandlerManager messageHandlerManager)
        {
            _messageHandlerManager = messageHandlerManager;
        }

        public IEnumerable<IMessageHandler> GetHandlers<TMessage>()
            where TMessage : class, IMessage
        {
            return GetHandlers(typeof(TMessage));
        }

        public IEnumerable<IMessageHandler> GetHandlers(Type messageType)
        {
            return _messageHandlerManager.MessageHandlerDict.ContainsKey(messageType)
                ? _messageHandlerManager.MessageHandlerDict[messageType]
                    .OrderByDescending(x => x.HandlerPriority)
                    .Select(s => s.Handler)
                    .ToList()
                : new List<IMessageHandler>();
        }
    }
}
