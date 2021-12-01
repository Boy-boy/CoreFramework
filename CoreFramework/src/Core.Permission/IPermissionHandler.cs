using System.Threading.Tasks;

namespace Core.Permission
{
    public interface IPermissionHandler
    {
        Task HandlerAsync(PermissionHandlerContext handlerContext);
    }
}
