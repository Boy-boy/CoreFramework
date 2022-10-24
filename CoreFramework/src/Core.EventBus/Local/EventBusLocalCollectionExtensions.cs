namespace Core.EventBus.Local
{
    public static class EventBusLocalCollectionExtensions
    {
        public static EventBusOptions AddLocalMq(this EventBusOptions options)
        {
            options.AddExtensions(new EventBusOptionsExtensions());
            return options;
        }
    }
}
