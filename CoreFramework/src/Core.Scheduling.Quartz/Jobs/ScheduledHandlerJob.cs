using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using global::Quartz;

namespace Core.Scheduling.Quartz.Jobs
{
    /// <summary>
    /// Quartz Job 适配器:把一次 trigger fire 转成对 <see cref="IScheduledHandler.ExecuteAsync"/> 的调用。
    /// </summary>
    /// <remarks>
    /// 单节点默认 DisallowConcurrentExecution(同 HandlerCode 串行);
    /// handler 描述符声明 <c>AllowConcurrentExecution=true</c> 时 Bootstrap 改用 <see cref="ConcurrentScheduledHandlerJob"/>。
    /// 跨节点互斥由 Quartz 集群锁(QRTZ_LOCKS)在抢 trigger 时保证。
    /// handler 异常先由 StateTrackingFilter 转 Faulted;只有 filter 链自身崩溃才会冒到这里,
    /// 此时重抛回 Quartz 让 JobListener / 失败统计 / misfire 重试感知。
    /// </remarks>
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public sealed class ScheduledHandlerJob : ScheduledHandlerJobBase
    {
        public ScheduledHandlerJob(
            IServiceScopeFactory scopeFactory,
            IScheduledHandlerRegistry registry,
            ILogger<ScheduledHandlerJob> logger)
            : base(scopeFactory, registry, logger) { }
    }

    /// <summary>允许并发的 Quartz Job 变体,行为同 <see cref="ScheduledHandlerJob"/> 但不带 DisallowConcurrentExecution。</summary>
    [PersistJobDataAfterExecution]
    public sealed class ConcurrentScheduledHandlerJob : ScheduledHandlerJobBase
    {
        public ConcurrentScheduledHandlerJob(
            IServiceScopeFactory scopeFactory,
            IScheduledHandlerRegistry registry,
            ILogger<ConcurrentScheduledHandlerJob> logger)
            : base(scopeFactory, registry, logger) { }
    }

    /// <summary>Quartz Job 适配器的公共基类。</summary>
    public abstract class ScheduledHandlerJobBase : IJob
    {
        /// <summary>JobDataMap 中 HandlerCode 的键。</summary>
        public const string HandlerCodeKey = "HandlerCode";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IScheduledHandlerRegistry _registry;
        private readonly ILogger _logger;

        protected ScheduledHandlerJobBase(
            IServiceScopeFactory scopeFactory,
            IScheduledHandlerRegistry registry,
            ILogger logger)
        {
            _scopeFactory = scopeFactory;
            _registry = registry;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var handlerCode = context.MergedJobDataMap.GetString(HandlerCodeKey);
            if (string.IsNullOrWhiteSpace(handlerCode))
            {
                _logger.LogError("Quartz job fired without {Key} in JobDataMap.", HandlerCodeKey);
                return;
            }

            var handler = _registry.Find(handlerCode);
            if (handler is null)
            {
                _logger.LogWarning("No IScheduledHandler matched code {Code}; job will be removed on next bootstrap.", handlerCode);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IHandlerExecutionPipeline>();

            var scheduledTime = context.ScheduledFireTimeUtc ?? context.FireTimeUtc;
            var executionContext = new HandlerExecutionContext(
                handlerCode,
                scheduledTime,
                context.FireTimeUtc,
                scope.ServiceProvider);

            try
            {
                await pipeline.InvokeAsync(handler, executionContext, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // StateTrackingFilter 应已把 handler 异常转成 Faulted,走到这里说明是 filter 链自身异常;
                // 抛回 Quartz 让 JobListener / 失败统计 / misfire 重试感知
                _logger.LogError(ex, "Quartz job adapter caught unhandled exception for {Code}; rethrowing to Quartz.", handlerCode);
                throw;
            }
        }
    }
}
