using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Configuration.Dashboard
{
    public class DashboardOptionsExtensions : IConfigurationOptionsExtensions
    {
        private readonly Action<DashboardOptions> _options;

        public DashboardOptionsExtensions(Action<DashboardOptions> options)
        {
            _options = options;
        }
        public void AddServices(IServiceCollection services)
        {
            services.AddTransient<IStartupFilter, DashboardStaticFileStartupFilter>();
            services.Configure(_options);
            services.TryAddSingleton(typeof(ServiceRouteBuilder));
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IDashboardRouteProvider), typeof(DashboardActionRouteProvider),ServiceLifetime.Singleton));
        }
    }
}
