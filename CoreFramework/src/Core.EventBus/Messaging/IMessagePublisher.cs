using System.Threading.Tasks;

namespace Core.EventBus
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message)
            where T: class, IMessage;
    }
}
