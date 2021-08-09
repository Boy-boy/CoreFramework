namespace Core.Configuration.Dashboard
{
    public class DashboardServiceMethodProvider<TService> : IServiceMethodProvider<TService> where TService : class
    {
        public void OnServiceMethodDiscovery(ServiceMethodProviderContext<TService> context)
        {
            var providerServiceBinder = new ProviderServiceBinder<TService>(context);
            var method = DashboardService.GetAllAsyncMethod();
            providerServiceBinder.AddMethod(method);
        }
    }
}
