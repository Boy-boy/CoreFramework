using System;
using System.Collections.Generic;

namespace Core.EventBus.Messaging
{
    public interface IMessageHandlerProvider
    {
        IEnumerable<IMessageHandlerWrapper> GetMessageHandlers(Type messageType);
    }
}
