namespace Core.Configuration.Dashboard
{
    public class DashboardServiceMethodProvider<TService> : IServiceMethodProvider<TService> where TService : class
    {
        public void OnServiceMethodDiscovery(ServiceMethodProviderContext<TService> context)
        {
            var providerServiceBinder = new ProviderServiceBinder<TService>(context);
            var method1 = DashboardService.GetAsyncMethod();
            var method2 = DashboardService.AddAsyncMethod();
            var method3 = DashboardService.UpdateAsyncMethod();
            var method4 = DashboardService.DeletedAsyncMethod();
            providerServiceBinder.AddMethod(method1);
            providerServiceBinder.AddMethod(method2);
            providerServiceBinder.AddMethod(method3);
            providerServiceBinder.AddMethod(method4);
        }
    }
}
