using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Core.Json.SystemTextJson;

namespace Core.Translate.BaiDu
{
    public class BaiDuTranslateProvider : ITranslateProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BaiDuTranslateProvider> _logger;
        private BaiDuTranslateOptions _options;
        private readonly object _lock = new();

        public BaiDuTranslateProvider(IOptionsMonitor<BaiDuTranslateOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<BaiDuTranslateProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _options = options.CurrentValue;
            options.OnChange((optionObject, _) =>
            {
                lock (_lock)
                {
                    _options = optionObject;
                }
            });
        }

        /// <summary>
        /// 翻译转换
        /// </summary>
        /// <param name="original"></param>
        /// <param name="query"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>若出现错误，将以original输出</returns>
        public async Task<List<string>> TranslateAsync(string original,
            TranslateQueryDto query,
            CancellationToken cancellationToken = default)
        {
            query ??= TranslateQueryDto.Default;

            string url;
            lock (_lock)
            {
                var salt = new Random().Next(0, int.MaxValue).ToString(); ;
                var sign = EncryptString($"{_options.AppId}{original}{salt}{_options.SecretKey}");
                var dictionary = new Dictionary<string, string>()
                {
                    {
                        "q",
                        UrlEncoder.Default.Encode(original)
                    },
                    {
                        "from",
                        query.From
                    },
                    {
                        "to",
                        query.To
                    },
                    {
                        "appid",
                        _options.AppId
                    },
                    {
                        "salt",
                         salt
                    },
                    {
                        "sign",
                         sign
                    }
                };
                url = AddQueryString(_options.Url, dictionary);
            }

            _logger.LogTrace($"baiDu translate url is {url}");

            var httpClient = _httpClientFactory.CreateClient("BaiDuTranslate");

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            var httpResponseMessage = await httpClient.SendAsync(requestMessage, cancellationToken);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                _logger.LogError($"the request failed,url is {url}");
                return new List<string>() { original };
            }

            var result = await httpResponseMessage.Content.ReadAsStringAsync();
            var data = result.ToObject<BaiDuTranslateResponseDto>();

            return data?.trans_result?.Select(p => p.Dst).ToList() ?? new List<string>() { original };
        }

        /// <summary>
        /// 计算MD5值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string EncryptString(string str)
        {
            var md5 = MD5.Create();
            // 将字符串转换成字节数组
            var byteOld = Encoding.UTF8.GetBytes(str);
            // 调用加密方法
            var byteNew = md5.ComputeHash(byteOld);
            // 将加密结果转换为字符串
            var sb = new StringBuilder();
            foreach (var b in byteNew)
            {
                // 将字节转换成16进制表示的字符串，
                sb.Append(b.ToString("x2"));
            }
            // 返回加密的字符串
            return sb.ToString();
        }

        /// <summary>
        /// url拼接
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private string AddQueryString(
            string uri,
            IEnumerable<KeyValuePair<string, string>> queryString)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));
            if (queryString == null)
                throw new ArgumentNullException(nameof(queryString));
            var num = uri.IndexOf('#');
            var str1 = uri;
            var str2 = "";
            if (num != -1)
            {
                str2 = uri.Substring(num);
                str1 = uri.Substring(0, num);
            }
            var flag = str1.IndexOf('?') != -1;
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(str1);
            foreach (var keyValuePair in queryString)
            {
                stringBuilder.Append(flag ? '&' : '?');
                stringBuilder.Append(keyValuePair.Key);
                stringBuilder.Append('=');
                stringBuilder.Append(keyValuePair.Value);
                flag = true;
            }
            stringBuilder.Append(str2);
            return stringBuilder.ToString();
        }
    }
}
