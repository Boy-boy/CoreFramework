using Core.EmailClient.Storage;
using Core.EmailClient.Storage.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EmailClient
{
    public abstract class EmailClientBase : IEmailClient
    {
        private readonly IEmailStorage _emailStorage;
        private readonly EmailSendMailBox _emailSendMailBox;

        protected EmailClientBase(IServiceScopeFactory serviceScopeFactory)
        {
            var provider = serviceScopeFactory.CreateScope().ServiceProvider;
            _emailStorage = provider.GetService<IEmailStorage>();
            _emailSendMailBox = ActivatorUtilities.CreateInstance<EmailSendMailBox>(provider, this, _emailStorage);
        }

        public async Task SendAsync(MailBodyEntity mailBodyEntity, CancellationToken cancellationToken = default)
        {
            if (_emailStorage == null)
            {
                _emailSendMailBox.EnqueueMessage(new MailBoxMessage(null, mailBodyEntity));
            }
            else
            {
                var id = await _emailStorage.AddAsync(ConvertToAddEmailModel(mailBodyEntity), cancellationToken: cancellationToken);
                _emailSendMailBox.EnqueueMessage(new MailBoxMessage(id, mailBodyEntity));
            }
        }

        public abstract Task PushAsync(MailBodyEntity mailBodyEntity, CancellationToken cancellationToken = default);

        private AddEmailModel ConvertToAddEmailModel(MailBodyEntity mailBodyEntity)
        {
            var addEmailModel = new AddEmailModel()
            {
                Sender = mailBodyEntity.Sender,
                SenderAddress = mailBodyEntity.SenderAddress,
                Recipients = mailBodyEntity.Recipients,
                Cc = mailBodyEntity.Cc,
                Bcc = mailBodyEntity.Bcc,
                Subject = mailBodyEntity.Subject,
                Body = mailBodyEntity.Body,
                BodyType = mailBodyEntity.BodyType,
                MailFiles = mailBodyEntity.MailFiles,
                LinkedResources = mailBodyEntity.LinkedResources,
                CreationUserId = mailBodyEntity.CreationUserId,
                IsSend = false
            };
            return addEmailModel;
        }
    }
}
