namespace Core.EmailClient
{
    public interface IEmailClient
    {
        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="mailBodyEntity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendAsync(MailBodyEntity mailBodyEntity, CancellationToken cancellationToken = default);
    }
}
