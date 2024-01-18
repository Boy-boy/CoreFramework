namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging.Model
{
    public class HttpRequestDto
    {
        public HttpRequestDto(HttpRequestMessage request)
        {
            Uri = request.RequestUri?.ToString();
            Method = request.Method.Method;
            Headers = request.Headers;
            Content = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult(); ;
        }

        public string Uri { get; set; }

        public string Method { get; set; }

        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers { get; set; }

        public string Content { get; set; }
    }
}
