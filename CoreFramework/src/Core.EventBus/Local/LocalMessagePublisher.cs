using Core.EventBus.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Local
{
    /// <summary>
    /// 本地（进程内）事件 publisher。
    /// </summary>
    /// <remarks>
    /// <para><b>与集成事件的区别</b></para>
    /// <list type="bullet">
    ///   <item><description>集成事件（<see cref="Integration.IIntegrationPublisher"/>）：跨进程，
    ///   走 broker，强一致性靠 outbox + inbox 保障；</description></item>
    ///   <item><description>本地事件：同进程同语言运行时，无 broker 中转，不存在网络 / 持久化失败问题，
    ///   因此不走 outbox，直接通过 <see cref="IMessageHandlerInvoker"/> 同步调用 handler。</description></item>
    /// </list>
    ///
    /// <para><b>事务行为</b></para>
    /// <para>
    /// 如果 invoker 是 <c>InboxAwareMessageHandlerInvoker</c>（即用户注册了 EfCore 存储），
    /// 本地 handler 也会被包在独立 transactional UoW 中执行 —— 与发布者的 UoW 是
    /// <b>两个独立事务</b>。这意味着发布者后续如果回滚，本地 handler 已经成功落库的数据不会跟着回滚。
    /// 如需"发布者 + handler 同事务"，应使用 saga 抽象而非 local event。
    /// </para>
    /// </remarks>
    public class LocalMessagePublisher : ILocalPublisher
    {
        private readonly ILogger<LocalMessagePublisher> _logger;
        private readonly ILocalMessageHandlerManager _handlerManager;
        private readonly IMessageHandlerInvoker _invoker;

        public LocalMessagePublisher(
            ILogger<LocalMessagePublisher> logger,
            ILocalMessageHandlerManager handlerManager,
            IMessageHandlerInvoker invoker)
        {
            _logger = logger;
            _handlerManager = handlerManager;
            _invoker = invoker;
        }

        /// <summary>
        /// 同步调用所有订阅该消息类型的 handler。
        /// 任意 handler 抛异常不会阻断其他 handler 的执行（已 catch + log）。
        /// </summary>
        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage
        {
            var messageType = message.GetType();

            // MessageHandlerWrappers 是不可变快照，可以安全枚举，不会被并发 Subscribe/UnSubscribe 影响
            var wrappers = _handlerManager.MessageHandlerWrappers
                .Where(p => p.MessageType == messageType)
                .OrderByDescending(p => p.HandlerPriority)
                .ToList();

            if (wrappers.Count == 0)
            {
                var messageName = MessageNameAttribute.GetNameOrDefault(messageType);
                _logger.LogWarning("No subscription for local memory message: {eventName}", messageName);

                _logger.LogTrace("Not subscribed to enable diagnostic listener,name is {name}", DiagnosticListenerConstants.NotSubscribed);
                EventBusDiagnosticListener.TracingNotSubscribed(message);
                return;
            }

            _logger.LogTrace("Enable diagnostic listeners before consume,name is {name}", DiagnosticListenerConstants.BeforeConsume);
            EventBusDiagnosticListener.TracingConsumeBefore(message);

            foreach (var wrapper in wrappers)
            {
                try
                {
                    await _invoker.InvokeAsync(messageType, wrapper.HandlerType, message, cancellationToken);
                }
                catch (System.Exception e)
                {
                    // 一个 handler 失败不影响其他 handler。本地事件没有"重投"机制，
                    // 失败的副作用就是该 handler 的工作没完成；调用方需自行考虑补偿
                    _logger.LogError(e,
                        "Local message processing failure: messageType={MessageType} handlerType={HandlerType}",
                        messageType, wrapper.HandlerType);
                    EventBusDiagnosticListener.TracingConsumeError(message, wrapper.HandlerType, e.Message);
                }
            }

            _logger.LogTrace("Enable diagnostic listeners after consume,name is {name}", DiagnosticListenerConstants.AfterConsume);
            EventBusDiagnosticListener.TracingConsumeAfter(message);
        }
    }
}
