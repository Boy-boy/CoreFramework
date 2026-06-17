using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus
{
    /// <summary>
    /// 消息发布者的统一契约。本地事件 / 集成事件分别派生为
    /// <see cref="Local.ILocalMessagePublisher"/> 与 <see cref="Integration.IIntegrationMessagePublisher"/>，
    /// 让业务侧能用同一套语义注入而不感知底层是 in-process 还是跨进程 broker。
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        /// 发布一条消息。
        /// </summary>
        /// <typeparam name="T">消息 CLR 类型，必须实现 <see cref="IMessage"/>。</typeparam>
        /// <param name="message">消息实例。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage;
    }
}
