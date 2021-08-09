using System;

namespace Core.Configuration.Dashboard
{
    public static class DashboardServiceCollectionExtensions
    {
        public static ConfigurationOptions AddDashboard(this ConfigurationOptions options, Action<DashboardOptions> actionOptions)
        {
            options.AddExtensions(new DashboardOptionsExtensions(actionOptions));
            return options;
        }
    }
}
