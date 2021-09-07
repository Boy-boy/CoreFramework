using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Permission.Storage
{
    public interface IPermissionRoleStorage
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<PageResultDto<RouteRoleEntity>> GetAsync(MessageQueryModel query, CancellationToken cancellationToken = default);

        Task<List<RouteRoleEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<int> GetCountAsync(MessageQueryModel query, CancellationToken cancellationToken);

        Task<int> UpdateAsync(ModifyMessageModel message, CancellationToken cancellationToken = default);
    }
}
