using System;

namespace Core.PersistentLogging.HttpClientFactory.PersistentLogging.Model
{
    public class PersistentLoggingDto
    {
        public PersistentLoggingDto(HttpRequestDto httpRequest,
            HttpResponseDto httpResponse)
        {
            HttpRequest = httpRequest ?? throw new ArgumentNullException(nameof(httpRequest));
            HttpResponse = httpResponse ?? throw new ArgumentNullException(nameof(httpResponse));
            CreateTime = DateTime.Now;
        }

        public HttpRequestDto HttpRequest { get; set; }

        public HttpResponseDto HttpResponse { get; set; }

        public Exception ActionException { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 方法执行异常
        /// </summary>
        /// <param name="exception"></param>
        public void ActionExecutionException(Exception exception)
        {
            ActionException = exception;
        }
    }
}
