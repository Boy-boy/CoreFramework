using Core.EventBus.Abstraction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Core.EventBus.Local
{
    public class EventBusLocal : EventBusBase, IDisposable
    {
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly IEventHandlerFactory _eventHandlerFactory;
        private readonly ILogger<EventBusLocal> _logger;

        public EventBusLocal(IEventBusSubscriptionsManager subsManager,
            IEventHandlerFactory eventHandlerFactory,
            ILogger<EventBusLocal> logger)
        {
            _subsManager = subsManager;
            _eventHandlerFactory = eventHandlerFactory;
            _logger = logger;
        }

        protected override void Publish(Type eventType, IntegrationEvent eventDate)
        {
            var exceptions = new List<Exception>();
            var eventName = EventNameAttribute.GetNameOrDefault(eventType);
            if (_subsManager.IncludeEventTypeForEventName(eventName))
            {
                var eventHandleTypes = _subsManager.TryGetEventHandlerTypes(eventName);
                foreach (var eventHandleType in eventHandleTypes)
                {
                    try
                    {
                        var handlerInstance = _eventHandlerFactory.GetHandler(eventHandleType);
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        concreteType.GetMethod("Handle").Invoke(handlerInstance, new object[] { eventDate });
                    }
                    catch (TargetInvocationException ex)
                    {
                        exceptions.Add(ex.InnerException);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }
            else
            {
                _logger.LogWarning("No subscription for local memory event: {eventName}", eventName);
            }
            if (exceptions.Any())
            {
                throw new AggregateException(
                    "More than one error has occurred while triggering the event: " + eventType, exceptions);
            }
        }

        protected override void Subscribe(Type eventType, Type handlerType)
        {
            _subsManager.AddSubscription(eventType, handlerType);
        }

        protected override void UnSubscribe(Type eventType, Type handlerType)
        {
            _subsManager.RemoveSubscription(eventType, handlerType);
        }

        public void Dispose()
        {
            _subsManager?.Dispose();
        }
    }
}
