using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Core.HttpClient
{
    public interface IBaseHttpClient
    {
        Task<HttpClientResponse> GetAsync(string url, Dictionary<string, string> requestHeaders, CancellationToken cancellationToken = default);

        Task<HttpClientResponse> PostAsync(string url, string content, CancellationToken cancellationToken = default);

        Task<HttpClientResponse> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default);
    }
}
