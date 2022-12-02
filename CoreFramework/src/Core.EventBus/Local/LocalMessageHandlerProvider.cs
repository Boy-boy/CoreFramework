namespace Core.EventBus.Local
{
    public class LocalMessageHandlerProvider : MessageHandlerProvider, ILocalMessageHandlerProvider
    {
        public LocalMessageHandlerProvider(ILocalMessageHandlerManager localMessageHandlerManager)
        : base(localMessageHandlerManager)
        {

        }
    }
}
