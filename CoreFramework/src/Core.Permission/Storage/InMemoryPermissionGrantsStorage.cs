using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Permission.Storage
{
    public class InMemoryPermissionGrantsStorage : IPermissionGrantsStorage
    {
        private static List<PermissionGrantsEntity> _entities;
        public InMemoryPermissionGrantsStorage()
        {
            _entities = new List<PermissionGrantsEntity>();
        }

        public async Task<List<PermissionGrantsEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_entities);
        }

        public async Task<PageResultDto<PermissionGrantsEntity>> GetAsync(MessageQueryModel query, CancellationToken cancellationToken = default)
        {
            var result = new PageResultDto<PermissionGrantsEntity>
            {
                Count = await GetCountAsync(query, cancellationToken),
                Items = string.IsNullOrWhiteSpace(query.Name)
                    ? _entities
                    : _entities.Where(e => e.Name.Contains(query.Name)).ToList()
            };

            return await Task.FromResult(result);
        }

        public async Task<int> GetCountAsync(MessageQueryModel query, CancellationToken cancellationToken)
        {
            return string.IsNullOrWhiteSpace(query.Name)
            ? await Task.FromResult(_entities.Count)
            : await Task.FromResult(_entities.Count(e => e.Name.Contains(query.Name)));
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
            entity.Value = message.Value;
            return await Task.FromResult(1);
        }
    }
}
