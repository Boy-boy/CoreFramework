using System;

namespace Core.Permission
{
    public class PermissionRoleRouteAttribute : Attribute
    {
        private readonly string _name;

        public PermissionRoleRouteAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            _name = name;
        }

        public string GetName()
        {
            return _name;
        }
    }
}
