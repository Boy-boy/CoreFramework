using System;
using System.Diagnostics.CodeAnalysis;

namespace Core.Uow
{
    public interface IDatabaseApiContainer
    {
        IDatabaseApi FindDatabaseApi([NotNull] string key);

        void AddDatabaseApi([NotNull] string key, [NotNull] IDatabaseApi api);

        IDatabaseApi GetOrAddDatabaseApi([NotNull] string key, [NotNull] Func<IDatabaseApi> factory);
    }
}
