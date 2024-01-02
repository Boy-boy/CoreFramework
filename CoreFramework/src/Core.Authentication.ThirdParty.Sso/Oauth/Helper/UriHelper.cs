using Microsoft.AspNetCore.Http;

namespace Core.Authentication.ThirdParty.Sso.Oauth.Helper
{
    public class UriHelper
    {
        /// <summary>
        /// 根据客户端ip获取对应的uri（若客户端ip是内网，则返回值是内网uri）
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="originalUris"></param>
        /// <returns></returns>
        public static string GetMatchingUriByClientHost(HttpContext httpContext, string[] originalUris)
        {
            if (originalUris == null || originalUris.Length == 0)
            {
                throw new ArgumentException("通过客户端Host获取目标uri失败，原始uris不存在！");
            }

            var uri = IpHelper.IsPrivateIp(httpContext.GetRealClientHost())
                ? originalUris.FirstOrDefault(IsPrivateUri)
                : originalUris.FirstOrDefault(uri => !IsPrivateUri(uri));

            return string.IsNullOrEmpty(uri)
                ? originalUris.First()
                : uri;
        }

        /// <summary>
        /// 是否是内网uri
        /// </summary>
        /// <param name="uriString"></param>
        /// <returns></returns>
        public static bool IsPrivateUri(string uriString)
        {
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
                return false;
            var host = uri.Host;
            return IpHelper.IsPrivateIp(host);
        }
    }
}
