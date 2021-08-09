using Core.Configuration.Dashboard.Service.dto;
using Core.Configuration.Storage;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Configuration.Dashboard
{
    public class DashboardService
    {
        private static readonly string _serviceName = nameof(DashboardService);

        private readonly IConfigurationStorage _configurationStorage;
        public DashboardService(IConfigurationStorage configurationStorage)
        {
            _configurationStorage = configurationStorage;
        }

        public async Task<List<ConfigurationMessage>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _configurationStorage.GetAsync(cancellationToken);
        }

        public async Task<ConfigurationMessage> GetAsync(MessageQueryDto query, CancellationToken cancellationToken)
        {
            return await _configurationStorage.GetAsync(query.Id, cancellationToken);
        }

        public async Task AddAsync(ConfigurationMessage message, CancellationToken cancellationToken)
        {
            await _configurationStorage.AddAsync(message, cancellationToken);
        }

        public async Task UpdateAsync(ConfigurationMessage message, CancellationToken cancellationToken)
        {
            await _configurationStorage.UpdateAsync(message, cancellationToken);
        }

        public async Task DeletedAsync(DeleteMessageDto dto, CancellationToken cancellationToken)
        {
            await _configurationStorage.DeletedAsync(dto.Id, cancellationToken);
        }

        public static Method<CancellationToken, List<ConfigurationMessage>> GetAllAsyncMethod()
        {
            return new Method<CancellationToken, List<ConfigurationMessage>>(_serviceName, "GetAllAsync","GET");
        }
    }
}
