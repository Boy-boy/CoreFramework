using Microsoft.AspNetCore.Authentication;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public class ThirdPartyAuthenticationCookieOptions : AuthenticationSchemeOptions
    {
        public TicketSerializer TicketSerializer { get; set; } = TicketSerializer.Default;
    }
}
