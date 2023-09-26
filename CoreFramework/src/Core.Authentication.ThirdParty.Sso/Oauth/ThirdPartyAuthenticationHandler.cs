using Core.Authentication.ThirdParty.Sso.Oauth.SignalR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public interface IThirdPartyAuthenticationHandler
    {
        string BuildSignedInRedirectUri(Dictionary<string, string> @params);

        string BuildSignedOutRedirectUri();
    }

    public abstract class ThirdPartyAuthenticationHandler<TOptions> : OAuthHandler<TOptions>, IThirdPartyAuthenticationHandler
        where TOptions : ThirdPartyOauthOptions, new()
    {
        public IServiceScopeFactory ServiceScopeFactory { get; }

        protected ThirdPartyAuthenticationHandler(IOptionsMonitor<TOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IServiceScopeFactory serviceScopeFactory)
            : base(options, logger, encoder, clock)
        {
            ServiceScopeFactory = serviceScopeFactory;
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
            if (!string.IsNullOrEmpty(Options.RemoteSignOutClearCookieName))
            {
                //TODO:使用SignalR通知前端删除cookie 
                await using var scope = ServiceScopeFactory.CreateAsyncScope();
                var serviceProvider = scope.ServiceProvider;
                var hubContext = serviceProvider.GetRequiredService<IHubContext<SignOutNotificationHub>>();
                await hubContext.Clients.All.SendAsync("DeleteCookie", Options.RemoteSignOutClearCookieName);
            }
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
