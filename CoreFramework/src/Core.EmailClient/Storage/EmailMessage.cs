namespace Core.EmailClient.Storage
{
    public class EmailMessage
    {
        /// <summary>
        /// id
        /// </summary>
        public string Id { get; set; }

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
        public string CreatorId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 是否发送
        /// </summary>
        public bool IsSend { get; set; }
    }
}
