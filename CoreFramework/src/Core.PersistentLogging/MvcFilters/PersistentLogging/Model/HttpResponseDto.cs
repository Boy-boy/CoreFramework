using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Core.PersistentLogging.MvcFilters.PersistentLogging.Model
{
    public class HttpResponseDto
    {
        public HttpResponseDto(HttpResponse httpResponse)
        {
            Headers = httpResponse.Headers;
            StatusCode = httpResponse.StatusCode;
            Body = GetResponseBody(httpResponse);
        }

        public IEnumerable<KeyValuePair<string, StringValues>> Headers { get; set; }

        public int StatusCode { get; set; }

        public string Body { get; set; }

        public string GetResponseBody(HttpResponse httpResponse)
        {
            if (!httpResponse.Body.CanRead)
                return null;

            httpResponse.Body.Position = 0;
            using var reader = new StreamReader(httpResponse.Body, leaveOpen: true);
            var bodyContent = reader.ReadToEndAsync().GetAwaiter().GetResult();
            httpResponse.Body.Position = 0;
            return bodyContent;
        }
    }
}
