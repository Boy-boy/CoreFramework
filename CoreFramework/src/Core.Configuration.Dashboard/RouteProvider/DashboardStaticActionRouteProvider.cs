namespace Core.Configuration.Dashboard
{
    public class DashboardStaticActionRouteProvider : IDashboardRouteProvider
    {
        public ServiceMethodProviderContext OnServiceMethodDiscovery()
        {
            var context = new ServiceMethodProviderContext();
            var methods = DashboardStaticActionRoute.GetMethods();
            foreach (var method in methods)
            {
                context.AddMethod(method);
            }
            return context;
        }
    }
}
