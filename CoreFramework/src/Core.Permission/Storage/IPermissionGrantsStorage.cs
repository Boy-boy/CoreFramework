using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Permission.Storage
{
    public interface IPermissionGrantsStorage
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<PageResultDto<PermissionGrantsEntity>> GetAsync(MessageQueryModel query, CancellationToken cancellationToken = default);

        Task<List<PermissionGrantsEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<int> GetCountAsync(MessageQueryModel query, CancellationToken cancellationToken);

        Task<int> UpdateAsync(ModifyMessageModel message, CancellationToken cancellationToken = default);
    }
}
