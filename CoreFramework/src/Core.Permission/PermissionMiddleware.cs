using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Core.Permission
{
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IPermissionRoleProvider _permissionRoleProvider;
        private readonly IPermissionHandler _permissionHandler;

        public PermissionMiddleware(
            RequestDelegate next,
            IPermissionRoleProvider permissionRoleProvider,
            IPermissionHandler permissionHandler)
        {
            _next = next;
            _permissionRoleProvider = permissionRoleProvider;
            _permissionHandler = permissionHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var endpoint = context.GetEndpoint();
            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                await _next(context);
                return;
            }
            var roles = new List<string>();

            var permissionNames = endpoint.Metadata.GetOrderedMetadata<PermissionRoleRouteAttribute>() ?? Array.Empty<PermissionRoleRouteAttribute>();
            if (permissionNames.Count > 0)
            {
                foreach (var permissionName in permissionNames)
                {
                    roles.AddRange(_permissionRoleProvider.GetRolesAsync(permissionName.GetName()));
                }
            }
            else
            {
                if (endpoint is RouteEndpoint routeEndpoint)
                {
                    var apiRoute = routeEndpoint.RoutePattern.RawText;
                    roles.AddRange(_permissionRoleProvider.GetRolesAsync(apiRoute));
                }
            }
            roles = roles.Distinct().ToList();

            var permissionContext = new PermissionContext(context, roles);
            var result = _permissionHandler.Handler(permissionContext);
            if (result.Forbidden)
            {
                context.Response.StatusCode = 403;
                return;
            }
            await _next(context);
        }
    }
}
