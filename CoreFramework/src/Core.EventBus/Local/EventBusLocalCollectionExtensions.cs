using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.EventBus.Local
{
    public static class EventBusLocalCollectionExtensions
    {
        public static EventBusBuilder AddLocalMq(this EventBusBuilder builder)
        {
            builder.Service.TryAddSingleton<IMessagePublisher, LocalMessagePublisher>();
            builder.Service.TryAddSingleton<IMessageSubscribe, LocalMessageSubscribe>();
            return builder;
        }

        public static EventBusOptions AddLocalMq(this EventBusOptions options)
        {
            options.AddExtensions(new EventBusLocalOptionsExtensions());
            return options;
        }
    }
}
