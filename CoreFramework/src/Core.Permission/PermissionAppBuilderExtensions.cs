using Microsoft.AspNetCore.Builder;
using System;

namespace Core.Permission
{
    public static class PermissionAppBuilderExtensions
    {
        public static IApplicationBuilder UsePermission(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            return app.UseMiddleware<PermissionMiddleware>();
        }
    }
}
