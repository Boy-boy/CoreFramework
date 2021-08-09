using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Core.Configuration.Dashboard
{
    public class ServerCallHandler<TService, TRequest, TResponse>
    {
        private readonly ServerMethod<TService, TRequest, TResponse> _invoker;

        public ServerCallHandler(ServerMethod<TService, TRequest, TResponse> invoker)
        {
            _invoker = invoker;
        }

        public Task HandleCallAsync(HttpContext httpContext)
        {

            return Task.CompletedTask;
        }
    }
}
