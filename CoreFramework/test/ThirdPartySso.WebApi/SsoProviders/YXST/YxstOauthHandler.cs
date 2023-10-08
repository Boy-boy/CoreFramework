using Core.Authentication.ThirdParty.Sso.Oauth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ThirdPartySso.WebApi.SsoProviders.YXST
{
    /// <summary>
    /// 远信数通
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class YxstOauthHandler<TOptions> : ThirdPartyAuthenticationHandler<TOptions>, IAuthenticationSignOutHandler
        where TOptions : YXSTOauthOptions, new()
    {
        public YxstOauthHandler(IOptionsMonitor<TOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        public virtual async Task SignOutAsync(AuthenticationProperties properties)
        {
            var token = await Context.GetTokenAsync(Options.SignOutScheme, "user_token");
            var parameters = new Dictionary<string, string>()
            {
                {"Token",token}
            };
            var redirectUri = QueryHelpers.AddQueryString(Options.EndSessionEndpoint, parameters!);
            await Options.Backchannel.GetAsync(redirectUri);

            Response.Redirect(BuildSignedOutRedirectUri());
            await Task.CompletedTask;
        }

        protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            var query = Request.Query;

            var info = query["info"];
            using var jsonDoc = JsonDocument.Parse(info);

            var state = jsonDoc.RootElement.GetProperty("state").GetString();
            var properties = Options.StateDataFormat.Unprotect(state);

            if (properties == null)
            {
                return HandleRequestResult.Fail("The oauth state was missing or invalid.");
            }

            // OAuth2 10.12 CSRF
            if (!ValidateCorrelationId(properties))
            {
                return HandleRequestResult.Fail("Correlation failed.", properties);
            }

            var loginState = jsonDoc.RootElement.GetProperty("LoginState").GetInt32();
            if (loginState != 200)
            {
                var msg = jsonDoc.RootElement.GetProperty("Msg").GetString()!;
                return await Task.FromResult(HandleRequestResult.Fail(msg));
            }

            var userName = jsonDoc.RootElement.GetProperty("UserName").GetString()!;
            var identity = new ClaimsIdentity(ClaimsIssuer);
            identity.AddClaims(new[]
            {
                new Claim(ClaimsIdentityDefault.UserName,userName),
                new Claim(ClaimsIdentityDefault.Account,userName)
            });

            properties.StoreTokens(new List<AuthenticationToken>()
            {
                new ()
                {
                    Name = "expires_at",
                    Value = DateTime.Now.AddHours(2).ToString(CultureInfo.InvariantCulture)
                },
                new ()
                {
                    Name = "user_token",
                    Value = jsonDoc.RootElement.GetProperty("UserToken").GetString()!
                },
                new ()
                {
                    Name = "access_token",
                    Value = jsonDoc.RootElement.GetProperty("AccessToken").GetString()!
                }
            });
            properties.RedirectUri = BuildSignedInRedirectUri(new Dictionary<string, string>()
            {
                {"target","/home"},
                {"ent","COMMON"},
                {"name",userName},
                {"account",userName},
            });

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), properties, Scheme.Name);
            return await Task.FromResult(HandleRequestResult.Success(ticket));
        }

        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            var accessToken = GetAccessTokenAsync().GetAwaiter().GetResult();
            var data = new
            {
                returnUrl = redirectUri,
                access_token = accessToken,
                state = Options.StateDataFormat.Protect(properties)
            };

            var uri = QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, new Dictionary<string, string>
            {
                {"info",JsonSerializer.Serialize(data)}
            });

            return uri;
        }

        #region private
        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var data = new
                {
                    OpenId = Options.ClientId,
                    ApplicationSecret = Options.ClientSecret
                };
                var content = JsonSerializer.Serialize(data);
                var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
                var httpResponseMessage = await Options.Backchannel.PostAsync(Options.AccessTokenEndpoint, stringContent);
                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    throw new Exception(await DisplayAsync(httpResponseMessage));
                }
                var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

                var response = JsonSerializer.Deserialize<YXSTAccessTokenEndpointResponseDto>(responseContent);
                if (response.ResultCode != 200)
                {
                    Logger.LogError(response.ResultMessage);
                }
                return response.AccessToken;
            }
            catch (Exception e)
            {
                Logger.LogError("get access token failure,error message:" + e.Message);
                throw;
            }
        }

        private static async Task<string> DisplayAsync(HttpResponseMessage response)
        {
            var output = new StringBuilder();
            output.Append("Status: " + response.StatusCode + ";");
            output.Append("Headers: " + response.Headers + ";");
            output.Append("Body: " + await response.Content.ReadAsStringAsync() + ";");
            return output.ToString();
        }
        #endregion

    }
}
