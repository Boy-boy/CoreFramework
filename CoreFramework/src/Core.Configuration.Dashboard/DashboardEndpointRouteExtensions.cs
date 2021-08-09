using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.Configuration.Dashboard
{
    public static class DashboardEndpointRouteExtensions
    {
        public static void MapDbConfigurationDashboard(this IEndpointRouteBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            builder.ServiceProvider.GetRequiredService<ServiceRouteBuilder<DashboardService>>().Build(builder);
        }
    }
}
