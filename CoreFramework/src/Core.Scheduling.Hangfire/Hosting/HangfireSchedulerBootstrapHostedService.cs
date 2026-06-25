using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Hangfire.Internal;
using Core.Scheduling.Hangfire.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using global::Hangfire;
using global::Hangfire.Storage;

namespace Core.Scheduling.Hangfire.Hosting
{
    /// <summary>
    /// 启动期把 <see cref="IScheduledHandler"/> 登记为 Hangfire RecurringJob 的宿主。
    /// <list type="bullet">
    /// <item>FixedInterval 用 <see cref="CronExpressionTranslator"/> 翻译成 cron(秒级会抛异常)</item>
    /// <item>Cron 描述符原样透传(Hangfire/Cronos 方言,5 字段分钟级或 6 字段秒级)</item>
    /// <item><see cref="ScheduleDescriptor.AllowConcurrentExecution"/> 决定选 sequential 还是 concurrent 方法</item>
    /// <item>cron / 时区 / 并发模式全部未变 → 跳过 AddOrUpdate,保留存储侧 NextExecution 与 Stats</item>
    /// <item>启用 <c>CleanupOrphanJobs</c> 时,清理"前缀匹配但注册表里没有"的孤儿 RecurringJob</item>
    /// </list>
    /// 启动期只读一次全量 RecurringJob,diff 与 cleanup 复用同一份快照。
    /// </summary>
    internal sealed class HangfireSchedulerBootstrapHostedService : IHostedService
    {
        private const string ConcurrentMethodName = nameof(ScheduledHandlerJobInvoker.ExecuteConcurrentAsync);
        private const string SequentialMethodName = nameof(ScheduledHandlerJobInvoker.ExecuteSequentialAsync);

        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IScheduledHandlerRegistry _registry;
        private readonly HangfireSchedulingOptions _options;
        private readonly ILogger<HangfireSchedulerBootstrapHostedService> _logger;

        public HangfireSchedulerBootstrapHostedService(
            IRecurringJobManager recurringJobManager,
            IScheduledHandlerRegistry registry,
            IOptions<HangfireSchedulingOptions> options,
            ILogger<HangfireSchedulerBootstrapHostedService> logger)
        {
            _recurringJobManager = recurringJobManager;
            _registry = registry;
            _options = options.Value;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var handlers = _registry.GetHandlers();

            // 一次性拉全量 RecurringJob，既给 diff 用又给 orphan cleanup 用，避免存储侧二次扫描
            using var connection = JobStorage.Current.GetConnection();
            var existingJobs = connection.GetRecurringJobs()
                .Where(j => !string.IsNullOrEmpty(j.Id))
                .ToDictionary(j => j.Id, j => j, StringComparer.OrdinalIgnoreCase);

            foreach (var handler in handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                UpsertRecurringJob(handler, existingJobs);
            }

            if (_options.CleanupOrphanJobs)
                CleanupOrphans(handlers, existingJobs);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private void UpsertRecurringJob(IScheduledHandler handler, Dictionary<string, RecurringJobDto> existingJobs)
        {
            var jobId = _options.JobIdPrefix + handler.HandlerCode;
            var cron = ResolveCron(handler);
            var timeZoneId = handler.Schedule.TimeZoneId;
            var concurrent = handler.Schedule.AllowConcurrentExecution;

            if (existingJobs.TryGetValue(jobId, out var existing)
                && IsSameDefinition(existing, cron, timeZoneId, concurrent))
            {
                // 定义未变,跳过 AddOrUpdate 以保留 NextExecution / LastExecution / Stats
                return;
            }

            var recurringOptions = new RecurringJobOptions();
            if (!string.IsNullOrWhiteSpace(timeZoneId))
                recurringOptions.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            if (concurrent)
            {
                _recurringJobManager.AddOrUpdate<ScheduledHandlerJobInvoker>(
                    jobId,
                    invoker => invoker.ExecuteConcurrentAsync(handler.HandlerCode, JobCancellationToken.Null),
                    cron,
                    recurringOptions);
            }
            else
            {
                _recurringJobManager.AddOrUpdate<ScheduledHandlerJobInvoker>(
                    jobId,
                    invoker => invoker.ExecuteSequentialAsync(handler.HandlerCode, JobCancellationToken.Null),
                    cron,
                    recurringOptions);
            }

            _logger.LogInformation(
                "Hangfire RecurringJob upserted: id={JobId}, cron={Cron}, concurrent={Concurrent}",
                jobId,
                cron,
                concurrent);
        }

        private static bool IsSameDefinition(RecurringJobDto existing, string cron, string? timeZoneId, bool concurrent)
        {
            if (!string.Equals(existing.Cron, cron, StringComparison.Ordinal)) return false;

            // Hangfire 存储里 TimeZoneId 缺省可能是 null 或 "UTC",按"都视为 UTC"对齐
            var leftTz = string.IsNullOrEmpty(existing.TimeZoneId) ? "UTC" : existing.TimeZoneId;
            var rightTz = string.IsNullOrEmpty(timeZoneId) ? "UTC" : timeZoneId;
            if (!string.Equals(leftTz, rightTz, StringComparison.OrdinalIgnoreCase)) return false;

            var expectedMethod = concurrent ? ConcurrentMethodName : SequentialMethodName;
            if (!string.Equals(existing.Job?.Method?.Name, expectedMethod, StringComparison.Ordinal)) return false;

            return true;
        }

        private static string ResolveCron(IScheduledHandler handler)
        {
            return handler.Schedule.Kind switch
            {
                ScheduleKind.Cron => handler.Schedule.CronExpression!,
                ScheduleKind.FixedInterval =>
                    CronExpressionTranslator.TranslateFixedInterval(handler.Schedule.Interval, handler.HandlerCode),
                _ => throw new InvalidOperationException($"Unsupported ScheduleKind: {handler.Schedule.Kind}"),
            };
        }

        private void CleanupOrphans(IReadOnlyCollection<IScheduledHandler> handlers, Dictionary<string, RecurringJobDto> existingJobs)
        {
            var aliveIds = new HashSet<string>(
                handlers.Select(h => _options.JobIdPrefix + h.HandlerCode),
                StringComparer.OrdinalIgnoreCase);

            foreach (var job in existingJobs.Values)
            {
                if (string.IsNullOrEmpty(job.Id)) continue;
                if (!job.Id.StartsWith(_options.JobIdPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                if (aliveIds.Contains(job.Id)) continue;

                _recurringJobManager.RemoveIfExists(job.Id);
                _logger.LogInformation(
                    "Hangfire orphan RecurringJob removed (no matching handler in registry): {JobId}",
                    job.Id);
            }
        }
    }
}
