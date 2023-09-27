using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public class ThirdPartyAuthenticationCookieHandler : SignInAuthenticationHandler<ThirdPartyAuthenticationCookieOptions>
    {
        private readonly IDataProtector _dataProtector;

        public ThirdPartyAuthenticationCookieHandler(
            IOptionsMonitor<ThirdPartyAuthenticationCookieOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IDataProtectionProvider dataProtectionProvider)
            : base(options, logger, encoder, clock)
        {
            _dataProtector = dataProtectionProvider.CreateProtector(nameof(ThirdPartyAuthenticationCookieHandler));
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Cookies.TryGetValue(CookieDefault.CookieName, out var cookieValue))
            {
                return AuthenticateResult.Fail(new Exception("第三方认证服务平台未认证！"));
            }
            var byteArray = _dataProtector.Unprotect(Convert.FromBase64String(cookieValue!));
            var ticket = Options.TicketSerializer.Deserialize(byteArray);
            return await Task.FromResult(AuthenticateResult.Success(ticket!));
        }

        protected override async Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
        {
            var byteArray = Options.TicketSerializer.Serialize(new AuthenticationTicket(user, properties, Scheme.Name));
            var cookieValue = Convert.ToBase64String(_dataProtector.Protect(byteArray));
            Response.Cookies.Append(CookieDefault.CookieName, cookieValue, Options.CookieBuilder.Build(Context));
            await Task.CompletedTask;
        }

        protected override async Task HandleSignOutAsync(AuthenticationProperties properties)
        {
            Response.Cookies.Delete(CookieDefault.CookieName);
            await Task.CompletedTask;
        }
    }
}
