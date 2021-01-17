using System.Threading.Tasks;

namespace Core.Messaging
{
    public interface IMessageHandler
    {
    }

    public interface IMessageHandler<in TMessage> : IMessageHandler
    where TMessage : class, IMessage
    {
        Task HandAsync(TMessage message);
    }
}
