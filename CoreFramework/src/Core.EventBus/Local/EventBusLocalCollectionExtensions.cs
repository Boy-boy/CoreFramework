using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus.Local
{
    public static class EventBusLocalCollectionExtensions
    {
        public static EventBusBuilder AddLocalMq(this EventBusBuilder builder)
        {
            builder.Service.AddSingleton<IMessagePublisher, LocalMessagePublisher>();
            builder.Service.AddSingleton<IMessageSubscribe, LocalMessageSubscribe>();
            return builder;
        }
    }
}
