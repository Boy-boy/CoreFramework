namespace Core.Configuration.Dashboard
{
    public class DashboardActionRouteProvider
    {
        public void OnServiceMethodDiscovery(ServiceMethodProviderContext<DashboardActionRoute> context)
        {
            var methods = DashboardActionRoute.GetMethods();
            foreach (var method in methods)
            {
                context.AddMethod(method);
            }
        }
    }
}
