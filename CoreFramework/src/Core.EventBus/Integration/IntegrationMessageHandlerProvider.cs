namespace Core.EventBus.Integration
{
    public class IntegrationMessageHandlerProvider : MessageHandlerProvider, IIntegrationMessageHandlerProvider
    {
        public IntegrationMessageHandlerProvider(IIntegrationMessageHandlerManager localMessageHandlerManager)
        : base(localMessageHandlerManager)
        {
        }
    }
}
