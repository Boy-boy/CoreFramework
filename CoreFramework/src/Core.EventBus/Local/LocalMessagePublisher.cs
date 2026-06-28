using Core.EventBus.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Local
{
    /// <summary>本地（进程内）事件 publisher；同步派发，不走 outbox。注册为 <b>Scoped</b> 生命周期。</summary>
    /// <remarks>
    /// <para><b>生命周期(Scoped)</b>:必须能拿到调用方 scope 的 SP 才能让 <see cref="ILocalMessageHandlerInvoker"/>
    /// 复用当前 scope 的 handler / DbContext / UoW。Singleton 拿不到 scope,Transient 又会被错误捕获到调用方的
    /// scope 容器里,Scoped 是唯一正确选项。详见 <see cref="ILocalMessageHandlerInvoker"/>。</para>
    /// <para><b>事务边界</b>:本地事件最终的事务归属由"发布者是否在 UoW 内"决定。</para>
    /// <list type="bullet">
    ///   <item><description><b>发布者在外层 UoW 内</b>(常见:业务在 UoW 里 publish + commit):
    ///   <see cref="LocalMessageHandlerInvoker"/> 用当前 scope 的 SP 直接 resolve handler,
    ///   handler 内拿到的 <c>DbContext</c> 自动加入外层 UoW → 与发布者业务<b>共享同一事务</b>。
    ///   发布者回滚会同时撤销 handler 数据;handler 抛错(本类聚合上抛)会让外层
    ///   <c>UoW.CommitAsync</c> 失败 → 业务回滚。</description></item>
    ///   <item><description><b>发布者在 UoW 外</b>(罕见:测试 / hosted service 直发):
    ///   无 ambient UoW → handler 内 <c>DbContext</c> 走自己的 SaveChanges 即时落库。</description></item>
    /// </list>
    /// <para>结论:同事务一致性在"发布者在外层 UoW 内"这条主路径上已具备,无需 saga;
    /// saga 仅在需要跨服务长事务 / 补偿 / 超时管理时才有必要。</para>
    /// <para>handler 失败处理:循环内 catch 收集,循环结束聚合 <see cref="AggregateException"/> 上抛。
    /// 与 broker subscriber 行为一致。业务想要 best-effort 语义,请在 handler 内 try-catch 吞掉异常。</para>
    /// </remarks>
    public class LocalMessagePublisher : ILocalPublisher
    {
        private readonly ILogger<LocalMessagePublisher> _logger;
        private readonly ILocalMessageHandlerManager _handlerManager;
        private readonly ILocalMessageHandlerInvoker _invoker;

        public LocalMessagePublisher(
            ILogger<LocalMessagePublisher> logger,
            ILocalMessageHandlerManager handlerManager,
            ILocalMessageHandlerInvoker invoker)
        {
            _logger = logger;
            _handlerManager = handlerManager;
            _invoker = invoker;
        }

        /// <summary>同步调用所有订阅的 handler;单个 handler 抛异常不阻断其他 handler,循环结束聚合 <see cref="AggregateException"/> 上抛。</summary>
        /// <exception cref="ArgumentException"><paramref name="message"/>.Id 为 <see cref="Guid.Empty"/>(inbox 去重键不可为空 Guid)。</exception>
        /// <exception cref="AggregateException">至少一个 handler 抛出异常时聚合所有错误上抛,让上游(通常是 UoW.CommitAsync)回滚业务事务。</exception>
        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, IMessage
        {
            if (message.Id == Guid.Empty)
                throw new ArgumentException("IMessage.Id 不能为空 Guid;Message 基类构造已分配 Guid.NewGuid(),自定义实现请确保 Id 唯一。", nameof(message));

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

            List<Exception> handlerErrors = null;
            foreach (var wrapper in wrappers)
            {
                try
                {
                    await _invoker.InvokeAsync(messageType, wrapper.HandlerType, message, cancellationToken);
                }
                catch (Exception e)
                {
                    // 当轮不阻断其他 handler,循环结束聚合上抛 → 上游 UoW.CommitAsync 拿到异常 → 回滚业务事务
                    _logger.LogError(e,
                        "Local message processing failure: messageType={MessageType} handlerType={HandlerType}",
                        messageType, wrapper.HandlerType);
                    EventBusDiagnosticListener.TracingConsumeError(message, wrapper.HandlerType, e.Message);
                    EventBusMetrics.HandlerFailures.Add(1,
                        new KeyValuePair<string, object>("messageName", MessageNameAttribute.GetNameOrDefault(messageType)),
                        new KeyValuePair<string, object>("handlerType", wrapper.HandlerType.FullName ?? wrapper.HandlerType.Name));
                    (handlerErrors ??= new List<Exception>()).Add(e);
                }
            }

            if (handlerErrors != null && handlerErrors.Count > 0)
            {
                // 不发 AfterConsume:监控混淆 ErrorConsume + AfterConsume 会误判为"消费成功"
                throw new AggregateException(
                    $"{handlerErrors.Count} local handler(s) failed for messageType={messageType.Name}",
                    handlerErrors);
            }

            _logger.LogTrace("Enable diagnostic listeners after consume,name is {name}", DiagnosticListenerConstants.AfterConsume);
            EventBusDiagnosticListener.TracingConsumeAfter(message);
        }
    }
}
