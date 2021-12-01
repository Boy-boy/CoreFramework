using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Permission
{
    public class PermissionAttribute : Attribute
    {
        private readonly string _permissions;

        public PermissionAttribute(string permissions)
        {
            if (string.IsNullOrWhiteSpace(permissions))
                throw new ArgumentNullException(nameof(permissions));
            _permissions = permissions;
        }

        public List<string> GetPermissions()
        {
            return _permissions.Split(',').ToList();
        }
    }
}
