using Core.Permission.Storage;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace Core.Permission
{
    public class DefaultPermissionRoleProvider : IPermissionRoleProvider
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IPermissionRoleStorage _permissionRoleStorage;

        public DefaultPermissionRoleProvider(IMemoryCache memoryCache, IPermissionRoleStorage permissionRoleStorage)
        {
            _memoryCache = memoryCache;
            _permissionRoleStorage = permissionRoleStorage;
            Initialization();
        }

        private void Initialization()
        {
            var entities = _permissionRoleStorage.GetAllAsync().GetAwaiter().GetResult();
            foreach (var entity in entities)
            {
                _memoryCache.Set(entity.ApiRoute, entity.Roles);
            }
        }

        public List<string> GetRolesAsync(string route)
        {
            if (route == null)
                return new List<string>();

            _memoryCache.TryGetValue(route, out var value);

            if (value is List<string> result)
            {
                return result;
            }
            return new List<string>();
        }
    }
}
