using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Integration
{
    public interface IOutBoxSender
    {
        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);
    }
}
