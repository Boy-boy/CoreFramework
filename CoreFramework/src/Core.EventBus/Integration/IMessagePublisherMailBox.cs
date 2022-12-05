namespace Core.EventBus.Integration
{
    public interface IMessagePublisherMailBox
    {
        void EnqueueMessage(IMessage message);
    }
}
