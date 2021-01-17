using System;
using System.Threading.Tasks;

namespace Core.Messaging
{
    public interface IMessageHandlerWrapper
    {
        Task HandlerAsync(IMessage message);

        Type HandlerType { get; }

        Type BaseHandlerType { get; }

        int HandlerPriority { get; }
    }
}
