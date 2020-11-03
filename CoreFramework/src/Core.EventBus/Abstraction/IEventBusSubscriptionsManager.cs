using System;
using System.Collections.Generic;

namespace Core.EventBus.Abstraction
{
    /// <summary>
    /// 消息订阅管理器
    /// </summary>
    public interface IEventBusSubscriptionsManager : IDisposable
    {
        event EventHandler<Type> OnEventRemoved;

        void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>, new();

        void RemoveSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>, new();


        void AddSubscription(Type eventType, Type handlerType);

        void RemoveSubscription(Type eventType, Type handlerType);

        bool IncludeSubscriptionsHandlesForEventName(string eventName);

        bool IncludeEventTypeForEventName(string eventName);

        List<Type> TryGetEventHandlerTypes(string eventName);

        Type TryGetEventTypeForEventName(string eventName);

    }
}
