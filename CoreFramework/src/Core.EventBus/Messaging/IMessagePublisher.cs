using System.Threading.Tasks;

namespace Core.Messaging
{
    public interface IMessagePublisher<in TMessage>
    where TMessage : class, IMessage
    {
        Task PublishAsync(TMessage message);
    }
}
