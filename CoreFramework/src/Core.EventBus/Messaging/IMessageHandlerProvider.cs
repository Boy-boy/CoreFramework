using System;
using System.Collections.Generic;

namespace Core.EventBus
{
    public interface IMessageHandlerProvider
    {
        IEnumerable<IMessageHandler> GetHandlers<TMessage>()
            where TMessage:class,IMessage;

        IEnumerable<IMessageHandler> GetHandlers(Type messageType);
    }
}
