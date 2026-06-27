using Core.EventBus.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Local
{
    /// <summary>本地（进程内）事件 publisher；同步派发，不走 outbox。</summary>
    /// <remarks>
    /// 若 invoker 是 inbox-aware 实现，本地 handler 会运行在与发布者 <b>独立</b> 的事务里；
    /// 发布者后续回滚不会撤销 handler 已落库的数据。需"发布者 + handler 同事务"应使用 saga。
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

        /// <summary>同步调用所有订阅的 handler；单个 handler 抛异常不会阻断其他 handler。</summary>
        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage
        {
            var messageType = message.GetType();

            // 不可变快照,可与并发 Subscribe/UnSubscribe 安全共存
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
                    // 本地事件无重投机制,失败的副作用是该 handler 工作未完成,补偿由调用方负责
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
