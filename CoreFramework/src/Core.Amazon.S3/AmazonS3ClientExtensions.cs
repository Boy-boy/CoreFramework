using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using System.Net;

namespace Core.Amazon.S3
{
    public static partial class AmazonS3ClientExtensions
    {
        /// <summary>
        /// 创建桶
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<bool> CreateBucketAsync(this AmazonS3Client client, string bucketName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException(nameof(bucketName));

            if (await AmazonS3Util.DoesS3BucketExistV2Async(client, bucketName).ConfigureAwait(false))
                return true;
            var putBucketRequest = new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true,
            };

            var response = await client.PutBucketAsync(putBucketRequest, cancellationToken).ConfigureAwait(false);

            return response.HttpStatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// 删除桶
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<bool> RemoveBucketAsync(this AmazonS3Client client, string bucketName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException(nameof(bucketName));

            if (!await AmazonS3Util.DoesS3BucketExistV2Async(client, bucketName).ConfigureAwait(false))
                return true;

            var response = await client.DeleteBucketAsync(bucketName, cancellationToken).ConfigureAwait(false);

            return response.HttpStatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// 获取签名url
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        /// <param name="keyName"></param>
        /// <param name="expires"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> GetPreSignedUrlAsync(this AmazonS3Client client, string bucketName, string keyName, DateTime expires, CancellationToken cancellationToken = default)
        {
            var getPreSigned = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = keyName,
                Expires = expires,
                Protocol = Protocol.HTTPS
            };
            var signUrl = await Task.Run(() => client.GetPreSignedURL(getPreSigned), cancellationToken);

            return signUrl;
        }

        /// <summary>
        /// 桶是否存在
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public static async Task<bool> ExistBucketAsync(this AmazonS3Client client, string bucketName)
        {
            Console.WriteLine("start check {0} exist whether ...", bucketName);

            var isExist = await ((IAmazonS3)client).DoesS3BucketExistAsync(bucketName);

            return isExist;
        }

        /// <summary>
        /// 获取对象流
        /// </summary>
        /// <param name="client"></param>
        /// <param name="keyName"></param>
        /// <param name="bucketName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<FileStream> GetObjectStreamAsync(this AmazonS3Client client, string bucketName, string keyName, CancellationToken cancellationToken = default)
        {
            var filePath = Path.GetTempFileName();
            using var fileTransferUtility = new TransferUtility(client);
            await fileTransferUtility.DownloadAsync(filePath, bucketName, keyName, cancellationToken);
            return new FileStream(filePath, FileMode.Open);
        }

        /// <summary>
        /// 上传对象流
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        /// <param name="keyName"></param>
        /// <param name="fileStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<bool> PutObjectStreamAsync(this AmazonS3Client client, string bucketName, string keyName, Stream fileStream, CancellationToken cancellationToken = default)
        {
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = keyName,
                InputStream = fileStream
            };
            var response = await client.PutObjectAsync(request, cancellationToken);
            return response.HttpStatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// 从s3获取指定长度数据
        /// </summary>
        /// <param name="amazonS3Client"></param>
        /// <param name="bucketName"></param>
        /// <param name="keyName"></param>
        /// <param name="start"></param>
        /// <param name="endInclusive"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<(Stream ResponseStream,long ContentLength)> GetRangeObjectStreamFromS3Async(
            this AmazonS3Client amazonS3Client,
            string bucketName,
            string keyName,
            long start,
            long endInclusive,
            CancellationToken cancellationToken = default)
        {
            if (endInclusive < 0)
            {
                return (new MemoryStream(), 0);
            }
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = keyName,
                ByteRange = new ByteRange(start, endInclusive)
            };
            var response = await amazonS3Client.GetObjectAsync(request, cancellationToken).
                ConfigureAwait(false);

            return (response.ResponseStream,response.ContentLength);

        }
    }
}
