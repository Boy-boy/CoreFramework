using System;

namespace Core.EventBus
{
    public interface IMessageHandlerWrapper
    {
        IMessageHandler Handler { get; }

        Type HandlerType { get; }

        Type BaseHandlerType { get; }

        int HandlerPriority { get; }
    }
}
