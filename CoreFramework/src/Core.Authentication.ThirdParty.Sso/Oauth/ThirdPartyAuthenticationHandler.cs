using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public abstract class ThirdPartyAuthenticationHandler<TOptions> : OAuthHandler<TOptions>
        where TOptions : ThirdPartyOauthOptions, new()
    {
        protected ThirdPartyAuthenticationHandler(IOptionsMonitor<TOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        public override Task<bool> HandleRequestAsync()
        {
            if (Options.RemoteSignOutPath.HasValue && Options.RemoteSignOutPath == Request.Path)
            {
                return HandleRemoteSignOutAsync();
            }

            return base.HandleRequestAsync();
        }

        protected virtual async Task<bool> HandleRemoteSignOutAsync()
        {
            await Context.SignOutAsync(Options.SignOutScheme);
            return true;
        }

        public virtual string BuildSignedInRedirectUri(Dictionary<string, string> @params)
        {
            return QueryHelpers.AddQueryString(BuildRedirectUriIfRelative(Options.SignedInRedirectUri), @params ?? new Dictionary<string, string>());
        }

        public virtual string BuildSignedOutRedirectUri()
        {
            return BuildRedirectUriIfRelative(Options.SignedOutRedirectUri);
        }

        /// <summary>
        /// 如果给定的路径是相对路径，则构建重定向路径
        /// </summary>
        protected string BuildRedirectUriIfRelative(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return uri;
            }

            if (!uri.StartsWith("/", StringComparison.Ordinal))
            {
                return uri;
            }

            return BuildRedirectUri(uri);
        }
    }
}
