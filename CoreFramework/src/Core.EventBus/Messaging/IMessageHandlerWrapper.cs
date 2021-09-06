using System;

namespace Core.EventBus
{
    public interface IMessageHandlerWrapper
    {
        IMessageHandler Handler { get; }

        string MessageName { get; }

        Type MessageType { get; }

        Type HandlerType { get; }

        int HandlerPriority { get; }
    }
}
