using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus
{
    /// <summary>
    /// 消息 handler 的标记接口;仅用于反射扫描 / 类型筛选。
    /// </summary>
    public interface IMessageHandler
    {
    }

    /// <summary>
    /// 强类型消息 handler 契约。
    /// </summary>
    /// <typeparam name="TMessage">本 handler 关心的消息类型。</typeparam>
    /// <remarks>
    /// handler 由 <see cref="IMessageHandlerInvoker"/> 在每次消费时从 DI scope 内 resolve:
    /// 可安全注入 Scoped 服务、不应持有跨调用的可变状态、异常由 invoker 转译并决定 ack/nack。
    /// </remarks>
    public interface IMessageHandler<in TMessage> : IMessageHandler
    where TMessage : class, IMessage
    {
        /// <summary>处理一条消息;反射定位依赖此方法名。</summary>
        Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
    }
}
