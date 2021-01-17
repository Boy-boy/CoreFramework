namespace Core.Messaging
{
    public interface IMessageSubscribe
    {
        void Subscribe<T, TH>()
            where T : class, IMessage
            where TH : IMessageHandler<T>, new();

        void UnSubscribe<T, TH>()
            where T : class, IMessage
            where TH : IMessageHandler<T>, new();
    }
}
