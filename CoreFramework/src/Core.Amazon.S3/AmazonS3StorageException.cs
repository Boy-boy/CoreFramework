using System;
using System.Net;

namespace Core.Amazon.S3
{
    public class AmazonS3StorageException : Exception
    {
        public HttpStatusCode Status { get; }
        public string Reason { get; }

        public AmazonS3StorageException(HttpStatusCode status, string reason, string message)
            : base($"Request to amazon s3 storage failed, status code: {(int)status}, reason: {(string.IsNullOrEmpty(reason) ? status.ToString() : reason)}, message:{message}")
        {
            Status = status;
            Reason = reason;
        }

        public AmazonS3StorageException(HttpStatusCode status, string reason, string message, Exception innerException)
            : base($"Request to amazon s3 storage failed, status code: {(int)status}, reason: {(string.IsNullOrEmpty(reason) ? status.ToString() : reason)}, message:{message}", innerException)
        {
            Status = status;
            Reason = reason;
        }
    }
}

