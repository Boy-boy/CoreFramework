using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Configuration.Storage
{
    public interface IConfigurationStorage
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<ConfigurationMessage> GetAsync(string id, CancellationToken cancellationToken = default);

        Task<List<ConfigurationMessage>> GetAsync(CancellationToken cancellationToken = default);

        Task AddAsync(ConfigurationMessage message, CancellationToken cancellationToken = default);

        Task UpdateAsync(ConfigurationMessage message, CancellationToken cancellationToken = default);

        Task DeletedAsync(string id, CancellationToken cancellationToken = default);
    }
}
