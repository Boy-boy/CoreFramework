using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus
{
    /// <summary>
    /// 抽象"如何调用一个消息 handler"。所有消费端入口（RabbitMQ subscriber、local publisher 等）
    /// 都不再直接反射调用 <c>HandAsync</c>，而是把"该怎么调"的责任委托给这个接口。
    /// </summary>
    /// <remarks>
    /// <para><b>为什么要这层抽象</b></para>
    /// <list type="bullet">
    ///   <item><description>不同模块需要不同的调用包装：
    ///     <list type="bullet">
    ///       <item><description><see cref="DefaultMessageHandlerInvoker"/>：只解决 DI scope，做最朴素的调用；</description></item>
    ///       <item><description><c>InboxAwareMessageHandlerInvoker</c>（在 <c>Core.EventBus.Storage.EfCore</c>）：
    ///       额外加上 UoW + inbox 去重 + 异常回滚；</description></item>
    ///       <item><description>未来可能加上"链路追踪 / 重试 / 限流"等装饰器。</description></item>
    ///     </list>
    ///   </description></item>
    ///   <item><description>调用方（subscriber）只面对 <see cref="InvokeAsync"/>，不感知背后是哪种实现，
    ///   也不感知是否启用了 inbox / UoW。注册策略由顶层 DI 配置决定。</description></item>
    /// </list>
    ///
    /// <para><b>替换约定</b></para>
    /// <para>
    /// 默认在 <c>AddEventBus</c> 中以 <c>TryAdd</c> 注册 <see cref="DefaultMessageHandlerInvoker"/>。
    /// 高级模块（如 <c>AddEfCoreEventBusStorage</c>）会先 <c>RemoveAll</c> 再注册自己的实现，
    /// 用以替换默认。
    /// </para>
    /// </remarks>
    public interface IMessageHandlerInvoker
    {
        /// <summary>
        /// 调用一个 handler 处理一条消息。
        /// </summary>
        /// <param name="messageType">
        /// 消息的 CLR 类型。用于通过反射定位 <c>IMessageHandler&lt;TMessage&gt;.HandAsync</c> 方法。
        /// </param>
        /// <param name="handlerType">
        /// handler 的 CLR 类型。<b>必须</b>是类型而非已解析实例，以便调用方在自己的 DI scope 内 resolve，
        /// 避免 scope 泄漏 / DbContext 错位。
        /// </param>
        /// <param name="message">消息实例（消费端通常已经过 JSON 反序列化为强类型对象）。</param>
        Task InvokeAsync(
            Type messageType,
            Type handlerType,
            IMessage message,
            CancellationToken cancellationToken = default);
    }
}
