using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Configuration.Storage
{
    public interface IConfigurationStorage
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<PageResultDto<ConfigurationMessage>> GetAsync(MessageQueryModel query, CancellationToken cancellationToken = default);

        Task<List<ConfigurationMessage>> GetAsync(string environment,string group, CancellationToken cancellationToken = default);

        Task<ConfigurationMessage> GetAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> ExistAsync(string key, CancellationToken cancellationToken);

        Task<int> GetCountAsync(CancellationToken cancellationToken);

        Task<int> AddAsync(CreateMessageModel message, CancellationToken cancellationToken = default);

        Task<int> UpdateAsync(ModifyMessageModel message, CancellationToken cancellationToken = default);

        Task<int> DeletedAsync(int id, CancellationToken cancellationToken = default);
    }
}
