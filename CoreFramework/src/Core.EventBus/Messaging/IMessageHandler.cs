using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus
{
    /// <summary>
    /// 消息 handler 的"标记接口"。仅用于反射扫描 / 类型筛选，
    /// 真正的业务约定在泛型形态 <see cref="IMessageHandler{TMessage}"/> 上。
    /// </summary>
    public interface IMessageHandler
    {
    }

    /// <summary>
    /// 强类型消息 handler 契约。每个业务订阅方实现本接口的一个/多个泛型实例化。
    /// </summary>
    /// <typeparam name="TMessage">本 handler 关心的消息类型。</typeparam>
    /// <remarks>
    /// <para>
    /// handler 由 <see cref="IMessageHandlerInvoker"/> 在每次消费时从 DI scope 内 resolve，
    /// 因此本接口的实现：
    /// <list type="bullet">
    ///   <item><description>注入 Scoped 服务（DbContext / Repository）是安全的；</description></item>
    ///   <item><description>不应持有跨调用的可变状态；</description></item>
    ///   <item><description>异常会被 invoker 转译并由消费端决定 ack/nack 行为。</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMessageHandler<in TMessage> : IMessageHandler
    where TMessage : class, IMessage
    {
        /// <summary>
        /// 处理一条消息。本方法实际是接口名的 typo 拼写（Hand），保留以兼容旧版调用；
        /// 反射定位也依赖此名称（参见 <see cref="DefaultMessageHandlerInvoker.ResolveHandleMethod"/>）。
        /// </summary>
        Task HandAsync(TMessage message, CancellationToken cancellationToken = default);
    }
}
