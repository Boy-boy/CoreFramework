namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging.Model
{
    public class HttpResponseDto
    {
        public HttpResponseDto(HttpResponseMessage httpResponse)
        {
            Headers = httpResponse.Headers;
            StatusCode = (int)httpResponse.StatusCode;
            IsSuccessStatusCode = httpResponse.IsSuccessStatusCode;
            Content = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers { get; set; }

        public int StatusCode { get; set; }

        public bool IsSuccessStatusCode { get; set; }

        public string Content { get; set; }
    }
}
