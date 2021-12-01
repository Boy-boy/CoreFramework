using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Permission
{
    public interface IPermissionHandlerProvider
    {
        Task<IEnumerable<IPermissionHandler>> GetHandlersAsync();
    }
}
