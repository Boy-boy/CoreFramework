using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public class ThirdPartyOauthOptions : OAuthOptions
    {
        public ThirdPartyOauthOptions()
        {
            CallbackPath = "/signin-callback";
            RemoteSignOutPath = "/signout-callback";
        }

        /// <summary>
        /// 登出终结点
        /// </summary>
        public string EndSessionEndpoint { get; set; } = default!;

        /// <summary>
        /// 远程登出回调地址
        /// </summary>
        public PathString RemoteSignOutPath { get; set; }

        /// <summary>
        /// 登出后重定向地址
        /// </summary>
        public string SignedOutRedirectUri { get; set; }

        /// <summary>
        /// 登入后重定向地址
        /// </summary>
        public string SignedInRedirectUri { get; set; }

        /// <summary>
        /// 登出scheme
        /// </summary>
        public string SignOutScheme { get; set; }
    }
}
