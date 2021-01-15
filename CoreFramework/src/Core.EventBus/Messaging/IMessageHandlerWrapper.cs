using System.Threading.Tasks;

namespace Core.EventBus.Messaging
{
    public interface IMessageHandlerWrapper
    {
        Task HandlerAsync(IMessage message);
    }
}
