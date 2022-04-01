using System;

namespace Core.Amazon.S3
{
    public class AmazonS3Options
    {
        public string ServiceURL { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public int RetryCount { get; set; } = 100;

        public TimeSpan? Timeout { get; set; } = TimeSpan.FromMinutes(60);

        public bool UseHttp { get; set; } = true;

        public bool ForcePathStyle { get; set; } = true;

        public string SignatureVersion { get; set; } = "v1";

        public int MaxConnectionsPerServer { get; set; } = int.MaxValue;

        public bool AllowAutoRedirect { get; set; } = false;
    }
}

