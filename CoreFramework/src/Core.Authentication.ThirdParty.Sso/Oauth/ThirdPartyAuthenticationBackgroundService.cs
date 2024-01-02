using Core.Authentication.ThirdParty.Sso.Oauth.Helper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Core.Authentication.ThirdParty.Sso.Oauth
{
    public class ThirdPartyAuthenticationBackgroundService : BackgroundService
    {
        private readonly ThirdPartyAuthenticationOptions _options;

        public ThirdPartyAuthenticationBackgroundService(
            IOptions<ThirdPartyAuthenticationOptions> options)
        {
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var customPrivateIps = _options.CustomPrivateIps?.Split(',') ?? Array.Empty<string>();
            IpHelper.CustomPrivateIpsEvent += s => s.AddRange(customPrivateIps);
            await Task.CompletedTask;
        }
    }
}
