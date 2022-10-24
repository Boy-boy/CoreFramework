using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Core.Permission
{
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IPermissionService _permissionService;

        public PermissionMiddleware(
            RequestDelegate next,
            IPermissionService permissionService)
        {
            _next = next;
            _permissionService = permissionService;
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

            var permissionAttributes = endpoint.Metadata.GetOrderedMetadata<PermissionAttribute>();
            if (!permissionAttributes.Any())
            {
                await _next(context);
                return;
            }

            var permissions = new List<string>();
            foreach (var permissionAttribute in permissionAttributes)
            {
                permissions = permissions.Union(permissionAttribute.GetPolicies()).ToList();
            }

            var permissionResult = await _permissionService.AuthorizeAsync(permissions);
            if (permissionResult.Forbidden)
            {
                context.Response.StatusCode = 403;
                return;
            }

            await _next(context);
        }
    }
}
