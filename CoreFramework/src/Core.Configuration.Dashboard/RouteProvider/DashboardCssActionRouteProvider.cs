namespace Core.Configuration.Dashboard
{
    public class DashboardCssActionRouteProvider : IDashboardRouteProvider
    {
        public ServiceMethodProviderContext OnServiceMethodDiscovery()
        {
            var context = new ServiceMethodProviderContext();
            var methods = DashboardCssActionRoute.GetMethods();
            foreach (var method in methods)
            {
                context.AddMethod(method);
            }
            return context;
        }
    }
}
