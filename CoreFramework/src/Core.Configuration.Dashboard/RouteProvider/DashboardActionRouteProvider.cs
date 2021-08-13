namespace Core.Configuration.Dashboard
{
    public class DashboardActionRouteProvider : IDashboardRouteProvider
    {
        public ServiceMethodProviderContext OnServiceMethodDiscovery()
        {
            var context = new ServiceMethodProviderContext();
            var methods = DashboardActionRoute.GetMethods();
            foreach (var method in methods)
            {
                context.AddMethod(method);
            }
            return context;
        }
    }
}
