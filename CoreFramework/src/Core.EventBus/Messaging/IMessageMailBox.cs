namespace Core.EventBus
{
    public interface IMessageMailBox
    {
        void EnqueueMessage(IMessage message);
    }
}
