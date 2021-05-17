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

        public static EventBusOptions AddLocalMq(this EventBusOptions options)
        {
            options.AddExtensions(new EventBusLocalOptionsExtensions());
            return options;
        }
    }
}
