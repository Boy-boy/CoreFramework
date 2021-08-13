namespace Core.Configuration.Dashboard
{
    public class DashboardHtmlActionRouteProvider : IDashboardRouteProvider
    {
        public ServiceMethodProviderContext OnServiceMethodDiscovery()
        {
            var context = new ServiceMethodProviderContext();
            var methods = DashboardHtmlActionRoute.GetMethods();
            foreach (var method in methods)
            {
                context.AddMethod(method);
            }
            return context;
        }
    }
}
