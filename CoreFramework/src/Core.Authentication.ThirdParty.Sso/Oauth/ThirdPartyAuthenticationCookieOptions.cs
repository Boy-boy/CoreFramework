using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public class ThirdPartyAuthenticationCookieOptions : AuthenticationSchemeOptions
    {
        public ThirdPartyAuthenticationCookieOptions()
        {
            CookieBuilder = new CookieBuilder()
            {
                Name = CookieDefault.CookieName,
                Path = CookieDefault.CookiePath,
                SameSite = SameSiteMode.Unspecified,
                HttpOnly = false,
                IsEssential = false,
            };
        }

        public TicketSerializer TicketSerializer { get; set; } = TicketSerializer.Default;

        public CookieBuilder CookieBuilder { get; set; }
    }
}
