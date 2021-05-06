using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.HttpClient
{
    public class BaseHttpClient : IBaseHttpClient
    {
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly ILogger<BaseHttpClient> _logger;

        public BaseHttpClient(
            System.Net.Http.HttpClient httpClient,
            ILogger<BaseHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<HttpClientResponse> GetAsync(string url, 
            Dictionary<string, string> requestHeaders, 
            CancellationToken cancellationToken = default)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            requestHeaders ??= new Dictionary<string, string>();
            foreach (var keyValuePair in requestHeaders)
            {
                requestMessage.Headers.Add(keyValuePair.Key, keyValuePair.Value);
            }
            return await SendAsync(requestMessage, cancellationToken);
        }

        public async Task<HttpClientResponse> PostAsync(string url, 
            string content,
            CancellationToken cancellationToken = default)
        {
            var requestContent = new StringContent(content, Encoding.UTF8, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url)) { Content = requestContent };
            return await SendAsync(requestMessage, cancellationToken);
        }

        public async Task<HttpClientResponse> SendAsync(HttpRequestMessage requestMessage,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadAsStringAsync();
                    return HttpClientResponse.Success(payload);
                }

                var error = "request failed: " + Display(response);
                _logger.LogError(error);
                return HttpClientResponse.Failed(new Exception(error));
            }
            catch (Exception e)
            {
                var error = e.Message;
                _logger.LogError(error);
                return HttpClientResponse.Failed(new Exception(error));
            }
        }

        private static async Task<string> Display(HttpResponseMessage response)
        {
            var output = new StringBuilder();
            output.Append("Status: " + response.StatusCode + ";");
            output.Append("Headers: " + response.Headers + ";");
            output.Append("Body: " + await response.Content.ReadAsStringAsync() + ";");
            return output.ToString();
        }
    }
}
