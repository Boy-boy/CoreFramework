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

        public async Task<int> AddAsync(AddMessageDto dto, CancellationToken cancellationToken)
        {
            var message = new ConfigurationMessage(dto.Key, dto.Value, dto.Description);
            return await _configurationStorage.AddAsync(message, cancellationToken);
        }

        public async Task<int> UpdateAsync(ConfigurationMessage message, CancellationToken cancellationToken)
        {
            return await _configurationStorage.UpdateAsync(message, cancellationToken);
        }

        public async Task<int> DeletedAsync(DeleteMessageDto dto, CancellationToken cancellationToken)
        {
            return await _configurationStorage.DeletedAsync(dto.Id, cancellationToken);
        }

        internal static Method<CancellationToken, List<ConfigurationMessage>> GetAllAsyncMethod()
        {
            return new Method<CancellationToken, List<ConfigurationMessage>>(_serviceName, "GetAllAsync", "POST");
        }

        internal static Method<MessageQueryDto, ConfigurationMessage> GetAsyncMethod()
        {
            return new Method<MessageQueryDto, ConfigurationMessage>(_serviceName, "GetAsync", "POST");
        }

        internal static Method<AddMessageDto, int> AddAsyncMethod()
        {
            return new Method<AddMessageDto, int>(_serviceName, "AddAsync", "POST");
        }

        internal static Method<ConfigurationMessage, int> UpdateAsyncMethod()
        {
            return new Method<ConfigurationMessage, int>(_serviceName, "UpdateAsync", "POST");
        }

        internal static Method<DeleteMessageDto, int> DeletedAsyncMethod()
        {
            return new Method<DeleteMessageDto, int>(_serviceName, "DeletedAsync", "POST");
        }

        public static List<IMethod> GetMethods()
        {
            var methods = new List<IMethod>
            {
               // new Method<CancellationToken, List<ConfigurationMessage>>(_serviceName, "GetAllAsync", "POST"),
                new Method<MessageQueryDto, ConfigurationMessage>(_serviceName, "GetAsync", "POST"),
                new Method<ConfigurationMessage, ConfigurationMessage>(_serviceName, "AddAsync", "POST"),
                new Method<ConfigurationMessage, ConfigurationMessage>(_serviceName, "UpdateAsync", "POST"),
                new Method<ConfigurationMessage, ConfigurationMessage>(_serviceName, "DeletedAsync", "POST")
            };
            return methods;
        }
    }
}
