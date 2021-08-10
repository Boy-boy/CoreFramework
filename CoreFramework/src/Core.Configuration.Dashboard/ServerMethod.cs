using System.Threading;
using System.Threading.Tasks;

namespace Core.Configuration.Dashboard
{
    public delegate Task<TResponse> ServerMethod<in TService, in TRequest, TResponse>(
        TService service,
        TRequest request,
        CancellationToken cancellationToken);
}
