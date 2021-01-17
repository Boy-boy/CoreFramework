using System.Threading.Tasks;

namespace Core.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync(IMessage message);
    }
}
