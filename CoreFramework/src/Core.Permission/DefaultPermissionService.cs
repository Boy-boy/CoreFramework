using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Core.Permission
{
    public class DefaultPermissionService : IPermissionService
    {
        private readonly IPermissionHandlerProvider _permissionHandlerProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<PermissionOptions> _options;

        public DefaultPermissionService(IPermissionHandlerProvider permissionHandlerProvider,
            IHttpContextAccessor httpContextAccessor,
            IOptions<PermissionOptions> options)
        {
            _permissionHandlerProvider = permissionHandlerProvider;
            _httpContextAccessor = httpContextAccessor;
            _options = options;
        }
        public async Task<PermissionResult> AuthorizeAsync(List<string> permissions)
        {
            var context = _httpContextAccessor.HttpContext;
            var permissionContext = new PermissionHandlerContext(context, permissions);

            var handlers = await _permissionHandlerProvider.GetHandlersAsync();
            foreach (var handler in handlers)
            {
                await handler.HandlerAsync(permissionContext);
                if (_options.Value.InvokeHandlersAfterFailure) continue;
                if (permissionContext.HasFailed)
                    break;
            }

            return permissionContext.HasSucceeded
                ? PermissionResult.Success()
                : PermissionResult.Forbid();
        }
    }
}
