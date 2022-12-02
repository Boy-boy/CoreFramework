using Core.Uow;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EntityFrameworkCore.UnitOfWork
{
    public class EfCoreUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<string, IDatabaseApi> _databaseApis;

        public EfCoreUnitOfWork(IServiceProvider serviceProvider)
        {
            _databaseApis = new Dictionary<string, IDatabaseApi>();
        }

        public void Commit()
        {
            foreach (var databaseApi in _databaseApis.Values)
            {
                if (databaseApi is ISupportsSavingChanges changes)
                {
                    changes.SaveChangesAsync().GetAwaiter().GetResult();
                }
            }
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            foreach (var databaseApi in _databaseApis.Values)
            {
                if (databaseApi is ISupportsSavingChanges changes)
                {
                    await changes.SaveChangesAsync(cancellationToken);
                }
            }
        }

        public IDatabaseApi FindDatabaseApi([NotNull] string key)
        {
            return _databaseApis.TryGetValue(key, out var obj)
                ? obj
                : default;
        }

        public void AddDatabaseApi([NotNull] string key, [NotNull] IDatabaseApi api)
        {
            if (_databaseApis.ContainsKey(key))
            {
                throw new Exception("There is already a database API in this unit of work with given key: " + key);
            }
            _databaseApis.Add(key, api);
        }

        public IDatabaseApi GetOrAddDatabaseApi([NotNull] string key, [NotNull] Func<IDatabaseApi> factory)
        {
            if (_databaseApis.TryGetValue(key, out var obj))
            {
                return obj;
            }
            return _databaseApis[key] = factory();
        }
    }
}
