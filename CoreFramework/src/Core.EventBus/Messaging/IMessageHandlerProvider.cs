using System;
using System.Collections.Generic;

namespace Core.Messaging
{
    public interface IMessageHandlerProvider
    {
        IEnumerable<IMessageHandlerWrapper> GetHandlers(Type messageType);

        void AddHandler(Type messageType, Type handlerType);

        void RemoveHandler(Type messageType, Type handlerType);
    }
}
