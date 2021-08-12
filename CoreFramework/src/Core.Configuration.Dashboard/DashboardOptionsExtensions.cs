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
            var option = new DashboardOptions();
            _options?.Invoke(option);
            services.AddSingleton(option);

            services.TryAddSingleton(typeof(ServiceRouteBuilder));
        }
    }
}
