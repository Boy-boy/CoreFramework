namespace Core.Configuration.Dashboard
{
    public class DashboardImgActionRouteProvider : IDashboardRouteProvider
    {
        public ServiceMethodProviderContext OnServiceMethodDiscovery()
        {
            var context = new ServiceMethodProviderContext();
            var methods = DashboardImgActionRoute.GetMethods();
            foreach (var method in methods)
            {
                context.AddMethod(method);
            }
            return context;
        }
    }

}
