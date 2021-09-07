using System.Collections.Generic;

namespace Core.Permission
{
    public interface IPermissionRoleProvider
   {
       List<string> GetRolesAsync(string route);
   }
}
