using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus
{
    /// <summary>
    /// 抽象"如何调用一个消息 handler";所有消费端入口都把调用细节委托给本接口。
    /// </summary>
    /// <remarks>
    /// 给上层留出装饰空间:默认实现仅做 scope + 反射调用;
    /// 启用存储的项目会替换为带 UoW / inbox 去重 / 异常回滚的实现。
    /// 默认以 TryAdd 注册,高级模块通过 RemoveAll + 重新注册替换。
    /// </remarks>
    public interface IMessageHandlerInvoker
    {
        /// <summary>调用一个 handler 处理一条消息。</summary>
        /// <param name="messageType">消息 CLR 类型;用于反射定位 <c>HandleAsync</c>。</param>
        /// <param name="handlerType">handler CLR 类型;必须是类型而非已解析实例,以便调用方在自己 DI scope 内 resolve。</param>
        /// <param name="message">消息实例(消费端通常已反序列化为强类型对象)。</param>
        Task InvokeAsync(
            Type messageType,
            Type handlerType,
            IMessage message,
            CancellationToken cancellationToken = default);
    }
}
