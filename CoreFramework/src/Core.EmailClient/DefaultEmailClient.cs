using MailKit.Net.Smtp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Core.EmailClient
{
    public class DefaultEmailClient : EmailClientBase
    {
        private readonly IOptions<EmailClientOptions> _options;
        private readonly ILogger<DefaultEmailClient> _logger;

        public DefaultEmailClient(
            IServiceScopeFactory serviceScopeFactory,
            IOptions<EmailClientOptions> options,
            ILogger<DefaultEmailClient> logger)
        : base(serviceScopeFactory)
        {
            _options = options;
            _logger = logger;
        }

        public override async Task PushAsync(MailBodyEntity mailBodyEntity, CancellationToken cancellationToken = default)
        {
            try
            {
                var smtpClient = CreateClient();
                var message = new MimeMessage();
                message.SetMailBaseMessage(mailBodyEntity)
                    .SetMailBodyMessage(mailBodyEntity);
                await smtpClient.SendAsync(message, cancellationToken);
                await smtpClient.DisconnectAsync(true, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError($"failed to send email,message is {e.Message}");
                throw;
            }
        }

        private ISmtpClient CreateClient()
        {
            //TODO:连接可能出现异常，后期需优化
            var option = _options.Value;
            var client = new SmtpClient();
            client.Connect(option.Host, option.Port, option.UseSsl);
            client.Authenticate(option.ClientId, option.ClientSecret);
            return client;
        }
    }
}
