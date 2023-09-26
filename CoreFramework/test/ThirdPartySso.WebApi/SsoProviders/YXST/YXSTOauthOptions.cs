using Core.Authentication.ThirdParty.Sso.Oauth;

namespace ThirdPartySso.WebApi.SsoProviders.YXST
{
    public class YXSTOauthOptions : ThirdPartyOauthOptions
    {
        public string AccessTokenEndpoint { get; set; }
    }
}
