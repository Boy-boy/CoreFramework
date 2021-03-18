using System.Reflection;

namespace Core.EventBus
{
    public interface IMessageSubscribe
    {
        void Initialize(params Assembly[] assemblies);

        void Subscribe<T, TH>()
            where T : class, IMessage
            where TH : IMessageHandler<T>, new();

        void UnSubscribe<T, TH>()
            where T : class, IMessage
            where TH : IMessageHandler<T>, new();
    }
}
