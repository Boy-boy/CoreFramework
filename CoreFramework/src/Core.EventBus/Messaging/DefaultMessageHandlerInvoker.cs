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
    /// <see cref="IMessageHandlerInvoker"/> 的默认实现:每条消息一个 DI scope,反射调用 <c>HandleAsync</c>。
    /// </summary>
    /// <remarks>
    /// 不开 UoW、不查 inbox,保留最朴素调用语义;反射 <see cref="TargetInvocationException"/> 解包后按原栈上抛。
    /// 需要事务一致性 + 幂等的项目应通过存储模块替换为 inbox-aware 实现。
    /// </remarks>
    public sealed class DefaultMessageHandlerInvoker : IMessageHandlerInvoker
    {
        // HandleAsync MethodInfo 缓存,避免每条消息都做 MakeGenericType + GetMethod
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
            // scope 与本次调用一一对应,离开 using 时正确释放 DbContext / IDisposable
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService(handlerType);
            var method = ResolveHandleMethod(messageType);

            try
            {
                await ((Task)method.Invoke(handler, [message, cancellationToken])!);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                // 解包反射调用包装的异常,按原异常 + 原栈重新抛出
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }
        }

        /// <summary>反射定位 <c>IMessageHandler&lt;TMessage&gt;.HandleAsync</c>,结果按 messageType 缓存。</summary>
        /// <remarks>public 是为了让 inbox-aware invoker 共享同一实现降低不一致风险。</remarks>
        public static MethodInfo ResolveHandleMethod(Type messageType)
        {
            return HandleMethodCache.GetOrAdd(messageType, static mt =>
            {
                var concreteType = typeof(IMessageHandler<>).MakeGenericType(mt);
                return concreteType.GetMethod("HandleAsync")
                    ?? throw new InvalidOperationException(
                        "未在 IMessageHandler<" + mt.Name + "> 找到 HandleAsync 方法。");
            });
        }
    }
}
