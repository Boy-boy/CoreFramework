namespace Core.EmailClient
{
    /// <summary>
    /// 邮件内容实体
    /// </summary>
    public class MailBodyEntity
    {
        /// <summary>
        /// 邮件创建人
        /// </summary>
        public string CreatorId { get; set; }

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
        public List<MailFile> MailFiles { get; set; } = new();

        /// <summary>
        /// 邮件图片集合
        /// </summary>
        public List<MailFile> LinkedResources { get; set; } = new();

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
        public List<string> Recipients { get; set; } = new();

        /// <summary>
        /// 抄送
        /// </summary>
        public List<string> Cc { get; set; } = new();

        /// <summary>
        /// 密送
        /// </summary>
        public List<string> Bcc { get; set; } = new();

        /// <summary>
        /// 邮件主题
        /// </summary>
        public string Subject { get; set; }
    }

    public class MailFile
    {
        public MailFile(string fileName, byte[] fileContent)
        {
            MailFileName = fileName;
            MailFileContent = fileContent;
        }

        /// <summary>
        /// 附件文件名称  例如：图片 MailFilePath=@"123.png"
        /// </summary>
        public string MailFileName { get; set; }

        /// <summary>
        /// 附件文件内容
        /// </summary>
        public byte[] MailFileContent { get; set; }
    }

    public enum MailTextFormat
    {
        /// <summary>An alias for the plain text format.</summary>
        Text = 0,
        /// <summary>The HTML text format.</summary>
        Html = 1,
    }
}
