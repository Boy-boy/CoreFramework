using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Core.Permission
{
    public class PermissionContext
    {
        private readonly HttpContext _httpContext;
        private readonly List<string> _routeRoles;

        public PermissionContext(HttpContext httpContext, List<string> routeRoles)
        {
            _httpContext = httpContext;
            _routeRoles = routeRoles;
        }
        public HttpContext HttpContext { get; set; }

        public List<string> RouteRoles { get; set; }
    }
}
