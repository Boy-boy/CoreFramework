using Core.Permission;
using System;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

            services.TryAddEnumerable(ServiceDescriptor.Scoped<IPermissionHandler, DefaultRolePermissionHandler>());
            services.TryAddScoped<IPermissionService, DefaultPermissionService>();
            services.AddHostedService<PermissionBackgroundService>();

            services.AddHttpContextAccessor();

            return services;
        }
    }
}
