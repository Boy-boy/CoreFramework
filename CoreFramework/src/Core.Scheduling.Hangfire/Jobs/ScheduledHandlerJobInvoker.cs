using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using global::Hangfire;

namespace Core.Scheduling.Hangfire.Jobs
{
    /// <summary>
    /// Hangfire 适配器 Job:把一次 RecurringJob 触发转成对 <see cref="IScheduledHandler.ExecuteAsync"/> 的调用。
    /// <para>
    /// 提供两个公开方法:
    /// <list type="bullet">
    /// <item><see cref="ExecuteSequentialAsync"/> 带
    ///   <see cref="DisableConcurrentExecutionAttribute"/>,本节点同一 HandlerCode 串行;</item>
    /// <item><see cref="ExecuteConcurrentAsync"/> 不带,允许并发。</item>
    /// </list>
    /// Bootstrap 期按 <see cref="ScheduleDescriptor.AllowConcurrentExecution"/> 选其中之一登记。
    /// 跨节点的互斥由 Hangfire 存储层分布式锁仲裁(同一 RecurringJob 同一时刻只一个节点抢到)。
    /// </para>
    /// </summary>
    public sealed class ScheduledHandlerJobInvoker
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IScheduledHandlerRegistry _registry;
        private readonly ILogger<ScheduledHandlerJobInvoker> _logger;

        public ScheduledHandlerJobInvoker(
            IServiceScopeFactory scopeFactory,
            IScheduledHandlerRegistry registry,
            ILogger<ScheduledHandlerJobInvoker> logger)
        {
            _scopeFactory = scopeFactory;
            _registry = registry;
            _logger = logger;
        }

        /// <summary>
        /// 非并发执行入口。lock 持有时长由
        /// <see cref="Hosting.HangfireSchedulingOptions.NonConcurrentLockTimeoutSeconds"/> 间接控制
        /// (Hangfire attribute 是编译期常量,这里硬编码 5 分钟,运行期不再读 options;
        /// 想改请在 options 同步调整以保持文档一致)。
        /// </summary>
        [DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
        public Task ExecuteSequentialAsync(string handlerCode, IJobCancellationToken cancellationToken)
            => InvokeCoreAsync(handlerCode, cancellationToken);

        /// <summary>并发执行入口(不加锁)。</summary>
        public Task ExecuteConcurrentAsync(string handlerCode, IJobCancellationToken cancellationToken)
            => InvokeCoreAsync(handlerCode, cancellationToken);

        private async Task InvokeCoreAsync(string handlerCode, IJobCancellationToken? jobCancellationToken)
        {
            if (string.IsNullOrWhiteSpace(handlerCode))
            {
                _logger.LogError("Hangfire job fired with empty handlerCode.");
                return;
            }

            var ct = jobCancellationToken?.ShutdownToken ?? CancellationToken.None;

            var handler = _registry.Find(handlerCode);
            if (handler is null)
            {
                _logger.LogWarning(
                    "No IScheduledHandler matched code {Code}; Hangfire RecurringJob will be removed on next bootstrap.",
                    handlerCode);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IHandlerExecutionPipeline>();

            var now = DateTimeOffset.UtcNow;
            var context = new HandlerExecutionContext(handlerCode, now, now, scope.ServiceProvider);

            try
            {
                await pipeline.InvokeAsync(handler, context, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hangfire job adapter caught unhandled exception for {Code}.", handlerCode);
                throw; // 抛回 Hangfire,让它按 AutomaticRetry filter 处理
            }
        }
    }
}
