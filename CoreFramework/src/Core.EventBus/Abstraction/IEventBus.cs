namespace Core.EventBus.Abstraction
{
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent eventData)
            where TEvent : IntegrationEvent;

        void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>,new();

        void UnSubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>, new();
    }
}
