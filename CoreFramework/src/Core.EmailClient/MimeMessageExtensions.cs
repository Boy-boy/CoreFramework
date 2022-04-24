using MimeKit;

namespace Core.EmailClient
{
    public static class MimeMessageExtensions
    {
        /// <summary>
        /// 设置邮件基础信息
        /// </summary>
        /// <param name="mimeMessage"></param>
        /// <param name="mailBodyEntity"></param>
        /// <returns></returns>
        public static MimeMessage SetMailBaseMessage(this MimeMessage mimeMessage, MailBodyEntity mailBodyEntity)
        {
            if (mimeMessage == null)
            {
                throw new ArgumentNullException(nameof(mimeMessage));
            }
            if (mailBodyEntity == null)
            {
                throw new ArgumentNullException(nameof(mailBodyEntity));
            }
            if (string.IsNullOrEmpty(mailBodyEntity.Subject))
            {
                throw new ArgumentNullException(nameof(mailBodyEntity.Subject));
            }
            if (string.IsNullOrEmpty(mailBodyEntity.SenderAddress))
            {
                throw new ArgumentNullException(nameof(mailBodyEntity.SenderAddress));
            }
            if (!mailBodyEntity.Recipients.Any())
            {
                throw new ArgumentException("at least one recipient");
            }



            //插入发件人
            mimeMessage.From.Add(new MailboxAddress(mailBodyEntity.Sender ?? mailBodyEntity.SenderAddress, mailBodyEntity.SenderAddress));

            //插入收件人
            if (mailBodyEntity.Recipients.Any())
            {
                foreach (var recipient in mailBodyEntity.Recipients)
                {
                    mimeMessage.To.Add(new MailboxAddress(recipient, recipient));
                }
            }

            //插入抄送人
            if (mailBodyEntity.Cc != null && mailBodyEntity.Cc.Any())
            {
                foreach (var cC in mailBodyEntity.Cc)
                {
                    mimeMessage.Cc.Add(new MailboxAddress(cC, cC));
                }
            }

            //插入密送人
            if (mailBodyEntity.Bcc != null && mailBodyEntity.Bcc.Any())
            {
                foreach (var bcc in mailBodyEntity.Bcc)
                {
                    mimeMessage.Bcc.Add(new MailboxAddress(bcc, bcc));
                }
            }

            //插入主题
            mimeMessage.Subject = mailBodyEntity.Subject;
            return mimeMessage;
        }

        /// <summary>
        /// 设置邮件body信息
        /// </summary>
        /// <param name="mimeMessage"></param>
        /// <param name="mailBodyEntity"></param>
        /// <returns></returns>
        public static MimeMessage SetMailBodyMessage(this MimeMessage mimeMessage, MailBodyEntity mailBodyEntity)
        {
            if (mimeMessage == null)
            {
                throw new ArgumentNullException(nameof(mimeMessage));
            }
            if (mailBodyEntity == null)
            {
                throw new ArgumentNullException(nameof(mailBodyEntity));
            }

            var builder = new BodyBuilder();
            switch (mailBodyEntity.BodyType)
            {
                case MailTextFormat.Text:
                    builder.TextBody = mailBodyEntity.Body;
                    break;
                case MailTextFormat.Html:
                    builder.HtmlBody = mailBodyEntity.Body;
                    break;
            }

            foreach (var linkedResource in mailBodyEntity.LinkedResources)
            {
                var image = builder.LinkedResources.Add(linkedResource.MailFileName, linkedResource.MailFileContent, ContentType.Parse(MimeTypes.GetMimeType(linkedResource.MailFileName)));
                image.ContentId = linkedResource.MailFileName;
            }

            foreach (var mailFile in mailBodyEntity.MailFiles)
            {
                var s = ContentType.Parse(MimeTypes.GetMimeType(mailFile.MailFileName));
                builder.Attachments.Add(mailFile.MailFileName, mailFile.MailFileContent, ContentType.Parse(MimeTypes.GetMimeType(mailFile.MailFileName)));
            }
            mimeMessage.Body = builder.ToMessageBody();
            return mimeMessage;
        }
    }
}
