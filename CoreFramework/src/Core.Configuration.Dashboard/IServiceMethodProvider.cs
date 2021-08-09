namespace Core.Configuration.Dashboard
{
    public interface IServiceMethodProvider<TService> where TService : class
    {
        void OnServiceMethodDiscovery(ServiceMethodProviderContext<TService> context);
    }
}
