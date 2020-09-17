using Core.EventBus.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.EventBus
{
    public class IocEventHandlerFactory : IEventHandlerFactory
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public IocEventHandlerFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        public IIntegrationEventHandler GetHandler(Type handlerType)
        {
            var scope = _serviceScopeFactory.CreateScope();
            return (IIntegrationEventHandler)scope.ServiceProvider.GetRequiredService(handlerType);
        }
    }
}
