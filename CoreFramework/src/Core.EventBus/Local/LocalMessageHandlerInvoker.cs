using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Local
{
    /// <summary><see cref="ILocalMessageHandlerInvoker"/> 的默认实现:在调用方 scope 内直接 resolve handler + 反射调用 <c>HandleAsync</c>。</summary>
    /// <remarks>
    /// <para>关键设计点:<b>不开新 scope、不开新 UoW、不查 inbox</b>。</para>
    /// <list type="bullet">
    ///   <item><description><b>共享 scope</b>:handler / DbContext / Repository 与发布者(外层业务)同一 scope,
    ///     避免 broker 风格 invoker 在外层 UoW 内"借出 DbContext → scope 释放 → DbContext 死"的危险。</description></item>
    ///   <item><description><b>共享 UoW</b>:外层有 UoW 时,handler 写入的 DB 变更跟外层业务<b>同事务</b>;
    ///     发布者回滚同时撤销 handler 数据。无外层 UoW 时,handler 走 DbContext 自己的 SaveChanges 即时落库。</description></item>
    ///   <item><description><b>不查 inbox</b>:本地事件不会"broker 重投",inbox 去重无意义。</description></item>
    /// </list>
    /// <para>Scoped 生命周期:每次 publish 时 publisher 通过 DI 拿到的是<b>调用方 scope</b>的 invoker 实例,
    /// 其 <c>IServiceProvider</c> 就是当前 scope 的 SP。</para>
    /// </remarks>
    public sealed class LocalMessageHandlerInvoker : ILocalMessageHandlerInvoker
    {
        private readonly IServiceProvider _serviceProvider;

        public LocalMessageHandlerInvoker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public async Task InvokeAsync(
            Type messageType,
            Type handlerType,
            IMessage message,
            CancellationToken cancellationToken = default)
        {
            // 用调用方 scope 的 SP resolve handler,handler 内拿到的 DbContext / UoW 与发布者业务共享
            var handler = _serviceProvider.GetRequiredService(handlerType);
            var method = DefaultMessageHandlerInvoker.ResolveHandleMethod(messageType);

            try
            {
                await ((Task)method.Invoke(handler, [message, cancellationToken])!);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                // 按原异常 + 原栈重新抛出,上层 LocalMessagePublisher 据此聚合错误
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }
        }
    }
}
