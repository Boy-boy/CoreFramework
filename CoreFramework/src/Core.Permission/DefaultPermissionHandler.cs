using System.Threading.Tasks;

namespace Core.Permission
{
    public class DefaultPermissionHandler : IPermissionHandler
    {
        public async Task HandlerAsync(PermissionHandlerContext handlerContext)
        {
            await Task.FromResult(PermissionResult.Success());
        }
    }
}
