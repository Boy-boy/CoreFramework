using System;

namespace Core.EventBus.Abstraction
{
    public interface IEventHandlerFactory
    {
        IIntegrationEventHandler GetHandler(Type handlerType);
    }
}
