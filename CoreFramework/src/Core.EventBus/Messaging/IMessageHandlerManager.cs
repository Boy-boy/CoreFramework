using System;
using System.Collections.Generic;

namespace Core.EventBus
{
    public interface IMessageHandlerManager
    {
        event EventHandler<Type> OnEventRemoved;
        IDictionary<Type, IList<IMessageHandlerWrapper>> MessageHandlerDict { get; }

        IDictionary<string, Type> MessageTypeMappingDict { get; }

        void AddHandler(Type messageType, Type handlerType);

        void RemoveHandler(Type messageType, Type handlerType);
    }
}
