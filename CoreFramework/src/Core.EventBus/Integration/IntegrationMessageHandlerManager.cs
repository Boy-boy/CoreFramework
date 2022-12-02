using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Integration
{
    public class IntegrationMessageHandlerManager : MessageHandlerManager, IIntegrationMessageHandlerManager
    {
        public IntegrationMessageHandlerManager(IServiceScopeFactory serviceScopeFactory)
        : base(serviceScopeFactory)
        {
        }
    }
}
