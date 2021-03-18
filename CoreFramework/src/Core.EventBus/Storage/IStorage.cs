using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Storage
{
    public interface IStorage
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        void StoreMessage(MediumMessage message, object dbTransaction = null);
    }
}
