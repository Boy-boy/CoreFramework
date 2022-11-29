using System.Threading;
using System.Threading.Tasks;

namespace Core.Uow
{
    public interface ISupportsSavingChanges
    {
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
