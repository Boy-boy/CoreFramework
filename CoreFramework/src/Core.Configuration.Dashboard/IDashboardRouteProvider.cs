namespace Core.Configuration.Dashboard
{
    public interface IDashboardRouteProvider
    {
        ServiceMethodProviderContext OnServiceMethodDiscovery();
    }
}
