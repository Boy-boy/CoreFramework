using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Local
{
    public class LocalMessageHandlerManager : MessageHandlerManager, ILocalMessageHandlerManager
    {
        public LocalMessageHandlerManager(IServiceScopeFactory serviceScopeFactory)
        : base(serviceScopeFactory)
        {
        }
    }
}
