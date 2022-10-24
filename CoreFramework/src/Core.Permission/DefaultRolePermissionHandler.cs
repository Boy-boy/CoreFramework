using Core.Permission.Storage;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Core.Permission
{
    public class DefaultRolePermissionHandler : IPermissionHandler
    {
        private readonly IPermissionGrantsStorage _storage;


        public DefaultRolePermissionHandler(IPermissionGrantsStorage storage)
        {
            _storage = storage;
        }

        public async Task HandlerAsync(PermissionHandlerContext handlerContext)
        {
            var roles = handlerContext.HttpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            if (!roles.Any())
            {
                await Task.FromResult(PermissionResult.Forbid());
                return;
            }

            //foreach (var role in roles.Distinct())
            //{
            //    if (await _storage.IsGrantedAsync(context.Permission.Name, Name, role))
            //    {
            //        return PermissionGrantResult.Granted;
            //    }
            //}

            await Task.FromResult(PermissionResult.Success());
        }
    }
}
