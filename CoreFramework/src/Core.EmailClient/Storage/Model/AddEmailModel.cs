namespace Core.EmailClient.Storage.Model
{
    public class AddEmailModel
    {
        /// <summary>
        /// 发件人
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// 发件人地址
        /// </summary>
        public string SenderAddress { get; set; }

        /// <summary>
        /// 收件人
        /// </summary>
        public List<string> Recipients { get; set; }

        /// <summary>
        /// 抄送
        /// </summary>
        public List<string> Cc { get; set; }

        /// <summary>
        /// 密送
        /// </summary>
        public List<string> Bcc { get; set; }

        /// <summary>
        /// 邮件主题
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// 邮件内容
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 邮件内容类型
        /// </summary>
        public MailTextFormat BodyType { get; set; } = MailTextFormat.Text;

        /// <summary>
        /// 邮件附件集合
        /// </summary>
        public List<MailFile> MailFiles { get; set; }

        /// <summary>
        /// 邮件正文图片集合
        /// </summary>
        public List<MailFile> LinkedResources { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreationUserId { get; set; }

        public bool IsSend { get; set; }
    }
}
