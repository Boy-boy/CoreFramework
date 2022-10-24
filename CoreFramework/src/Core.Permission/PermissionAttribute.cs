using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Permission
{
    public class PermissionAttribute : Attribute
    {
        private readonly string _policy;

        public PermissionAttribute(string policy)
        {
            if (string.IsNullOrWhiteSpace(policy))
                throw new ArgumentNullException(nameof(policy));
            _policy = policy;
        }

        public List<string> GetPolicies()
        {
            return _policy.Split(',').ToList();
        }
    }
}
