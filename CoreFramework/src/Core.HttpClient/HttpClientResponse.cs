using System;

namespace Core.HttpClient
{
    public class HttpClientResponse
    {
        public string Result { get; set; }

        public Exception Error { get; set; }

        public HttpClientResponse(string result)
        {
            Result = result;
        }

        public HttpClientResponse(Exception error)
        {
            Error = error;
        }

        public static HttpClientResponse Success(string result)
        {
            return new HttpClientResponse(result);
        }

        public static HttpClientResponse Failed(Exception error)
        {
            return new HttpClientResponse(error);
        }
    }
}
