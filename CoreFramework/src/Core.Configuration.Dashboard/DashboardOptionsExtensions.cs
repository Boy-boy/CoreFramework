using Microsoft.Extensions.DependencyInjection;
using System;
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
         
            services.Configure(_options);
            services.TryAddSingleton(typeof(ServiceRouteBuilder));
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IDashboardRouteProvider), typeof(DashboardActionRouteProvider),ServiceLifetime.Singleton));
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IDashboardRouteProvider), typeof(DashboardHtmlActionRouteProvider), ServiceLifetime.Singleton));
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IDashboardRouteProvider), typeof(DashboardCssActionRouteProvider), ServiceLifetime.Singleton));
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IDashboardRouteProvider), typeof(DashboardJavaScriptActionRouteProvider), ServiceLifetime.Singleton));
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IDashboardRouteProvider), typeof(DashboardImgActionRouteProvider), ServiceLifetime.Singleton));
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IDashboardRouteProvider), typeof(DashboardStaticActionRouteProvider), ServiceLifetime.Singleton));
        }
    }
}
