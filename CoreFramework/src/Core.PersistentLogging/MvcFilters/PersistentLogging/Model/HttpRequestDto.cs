using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Core.PersistentLogging.MvcFilters.PersistentLogging.Model
{
    public class HttpRequestDto
    {
        public HttpRequestDto(HttpRequest request)
        {
            Protocol = request.Protocol;
            Method = request.Method;
            Scheme = request.Scheme;
            PathBase = request.PathBase;
            Path = request.Path;
            Headers = request.Headers;
            QueryString = request.QueryString.ToString();
            Body = GetRequestBody(request);
        }

        public string Protocol { get; set; }

        public string Method { get; set; }

        public string Scheme { get; set; }

        public string PathBase { get; set; }

        public string Path { get; set; }

        public IEnumerable<KeyValuePair<string, StringValues>> Headers { get; set; }

        public string QueryString { get; set; }

        public string Body { get; set; }

        private string GetRequestBody(HttpRequest httpRequest)
        {
            httpRequest.EnableBuffering();
            using var reader = new StreamReader(httpRequest.Body, leaveOpen: true);
            var bodyContent = reader.ReadToEndAsync().GetAwaiter().GetResult();
            httpRequest.Body.Position = 0;
            return bodyContent;
        }
    }
}
