using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Permission.Storage
{
    public class InMemoryPermissionRoleStorage : IPermissionRoleStorage
    {
        private static List<RouteRoleEntity> _entities;
        public InMemoryPermissionRoleStorage()
        {
            _entities = new List<RouteRoleEntity>();
        }

        public async Task<List<RouteRoleEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_entities);
        }

        public async Task<PageResultDto<RouteRoleEntity>> GetAsync(MessageQueryModel query, CancellationToken cancellationToken = default)
        {
            var result = new PageResultDto<RouteRoleEntity>();

            if (string.IsNullOrWhiteSpace(query.ApiRoute))
            {
                result.Count = _entities.Count;
                result.Items = _entities;
            }
            result.Count = await GetCountAsync(query, cancellationToken);
            result.Items = _entities.Where(e => e.ApiRoute.StartsWith(query.ApiRoute)).ToList();

            return await Task.FromResult(result);
        }

        public async Task<int> GetCountAsync(MessageQueryModel query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query.ApiRoute))
            {
                return await Task.FromResult(_entities.Count);
            }
            return await Task.FromResult(_entities.Count(e => e.ApiRoute.StartsWith(query.ApiRoute)));
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        public async Task<int> UpdateAsync(ModifyMessageModel message, CancellationToken cancellationToken = default)
        {
            if (_entities.All(e => e.Id != message.Id))
                return await Task.FromResult(0);

            var entity = _entities.First(e => e.Id == message.Id);
            entity.Roles = message.Roles;
            return await Task.FromResult(1);
        }
    }
}
