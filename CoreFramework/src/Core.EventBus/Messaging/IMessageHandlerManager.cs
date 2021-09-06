using System;
using System.Collections.Generic;

namespace Core.EventBus
{
    public interface IMessageHandlerManager
    {
        event EventHandler<Type> OnEventRemoved;

        IList<IMessageHandlerWrapper> MessageHandlerWrappers { get; }

        void AddHandler(Type messageType, Type handlerType);

        void RemoveHandler(Type messageType, Type handlerType);
    }
}
