using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Permission
{
    public interface IPermissionService
    {
        Task<PermissionResult> AuthorizeAsync(List<string> permissions);
    }
}
