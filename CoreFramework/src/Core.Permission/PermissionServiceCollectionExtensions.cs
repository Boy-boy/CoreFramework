using Core.Permission;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PermissionServiceCollectionExtensions
    {
        public static IServiceCollection AddPermission(this IServiceCollection services, Action<PermissionOptions> optionAction)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (optionAction == null)
                throw new ArgumentNullException(nameof(optionAction));

            var options = new PermissionOptions();
            optionAction.Invoke(options);
            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }

            services.AddScoped<IPermissionHandler, DefaultPermissionHandler>();
            services.AddSingleton<IPermissionRoleProvider, DefaultPermissionRoleProvider>();
            services.AddMemoryCache();
            services.AddHostedService<PermissionBackgroundService>();

            return services;
        }
    }
}
