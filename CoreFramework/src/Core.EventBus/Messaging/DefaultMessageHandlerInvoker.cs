using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus
{
    /// <summary>
    /// <see cref="IMessageHandlerInvoker"/> 的默认实现：
    /// 为每条消息创建一个 DI scope，在 scope 内 resolve handler 并反射调用 <c>HandAsync</c>。
    /// </summary>
    /// <remarks>
    /// <para><b>语义</b></para>
    /// <list type="bullet">
    ///   <item><description>不开 UoW、不查 inbox：保留"最朴素调用"语义，给"不在乎事务一致性 / 已自行管 UoW"的项目使用</description></item>
    ///   <item><description>仍然创建独立 scope，确保 handler 的 DI 生命周期与本次消费一一对应（修复旧版 scope 泄漏）</description></item>
    ///   <item><description>反射调用抛出的 <see cref="TargetInvocationException"/> 被解包，业务异常按原样上抛、保留原始栈</description></item>
    /// </list>
    /// <para>
    /// 需要事务一致性 + 幂等的项目应该在启动时通过 <c>options.AddEfCoreEventBusStorage&lt;TDbContext&gt;()</c>
    /// 注册 <c>InboxAwareMessageHandlerInvoker</c> 替换本实现。
    /// </para>
    /// </remarks>
    public sealed class DefaultMessageHandlerInvoker : IMessageHandlerInvoker
    {
        // 每个 messageType 的 HandAsync MethodInfo 缓存。避免每条消息都做 MakeGenericType + GetMethod
        private static readonly ConcurrentDictionary<Type, MethodInfo> HandleMethodCache = new();

        private readonly IServiceScopeFactory _scopeFactory;

        public DefaultMessageHandlerInvoker(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <inheritdoc />
        public async Task InvokeAsync(
            Type messageType,
            Type handlerType,
            IMessage message,
            CancellationToken cancellationToken = default)
        {
            // 关键：在 scope 内 resolve handler，scope 与本次调用一一对应；
            // 离开 using 时 scope 释放，确保 DbContext / 其它 IDisposable 服务被正确释放
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService(handlerType);
            var method = ResolveHandleMethod(messageType);

            try
            {
                await ((Task)method.Invoke(handler, [message, cancellationToken])!);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                // 反射调用抛出的异常被包了一层；按原异常 + 原栈重新抛出
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }
        }

        /// <summary>
        /// 通过反射定位 <c>IMessageHandler&lt;TMessage&gt;.HandAsync(TMessage, CancellationToken)</c>，
        /// 结果按 messageType 缓存，避免每条消息都做一次反射查找。
        /// </summary>
        /// <remarks>
        /// 之所以 public：<c>InboxAwareMessageHandlerInvoker</c> 也需要这个方法，
        /// 通过共享降低不一致风险。
        /// </remarks>
        public static MethodInfo ResolveHandleMethod(Type messageType)
        {
            return HandleMethodCache.GetOrAdd(messageType, static mt =>
            {
                var concreteType = typeof(IMessageHandler<>).MakeGenericType(mt);
                return concreteType.GetMethod("HandAsync")
                    ?? throw new InvalidOperationException(
                        "未在 IMessageHandler<" + mt.Name + "> 找到 HandAsync 方法。");
            });
        }
    }
}
