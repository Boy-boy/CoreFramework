using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Quartz.Jobs;
using Core.Scheduling.Quartz.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using global::Quartz;
using global::Quartz.Impl.Matchers;

namespace Core.Scheduling.Quartz.Hosting
{
    /// <summary>启动期 Job / Trigger 登记宿主。</summary>
    /// <remarks>
    /// 遍历 <see cref="IScheduledHandlerRegistry"/>:
    /// <list type="bullet">
    /// <item>FixedInterval → SimpleTrigger;Cron → CronTrigger;</item>
    /// <item>差量调度:trigger 已存在且 schedule 未变 → 不动(保留 NEXT_FIRE_TIME / TIMES_TRIGGERED),变了 → Reschedule;</item>
    /// <item><c>CleanupOrphanJobs</c> 开启时清理注册表里没有的孤儿 Job;</item>
    /// <item>handler 描述符 <c>AllowConcurrentExecution=true</c> 用 <see cref="ConcurrentScheduledHandlerJob"/>,
    /// 否则用 <see cref="ScheduledHandlerJob"/>(带 DisallowConcurrentExecution)。</item>
    /// </list>
    /// </remarks>
    internal sealed class QuartzSchedulerBootstrapHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IScheduledHandlerRegistry _registry;
        private readonly QuartzSchedulingOptions _quartzOptions;
        private readonly ILogger<QuartzSchedulerBootstrapHostedService> _logger;

        public QuartzSchedulerBootstrapHostedService(
            ISchedulerFactory schedulerFactory,
            IScheduledHandlerRegistry registry,
            IOptions<QuartzSchedulingOptions> quartzOptions,
            ILogger<QuartzSchedulerBootstrapHostedService> logger)
        {
            _schedulerFactory = schedulerFactory;
            _registry = registry;
            _quartzOptions = quartzOptions.Value;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);
            var handlers = _registry.GetHandlers();

            foreach (var handler in handlers)
            {
                await UpsertJobAndTriggerAsync(scheduler, handler, cancellationToken).ConfigureAwait(false);
            }

            if (_quartzOptions.CleanupOrphanJobs)
            {
                await CleanupOrphansAsync(scheduler, handlers, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task UpsertJobAndTriggerAsync(IScheduler scheduler, IScheduledHandler handler, CancellationToken ct)
        {
            var jobKey = new JobKey(handler.HandlerCode, _quartzOptions.JobGroup);
            var triggerKey = new TriggerKey(handler.HandlerCode, _quartzOptions.JobGroup);
            var schedule = handler.Schedule;

            var jobType = schedule.AllowConcurrentExecution
                ? typeof(ConcurrentScheduledHandlerJob)
                : typeof(ScheduledHandlerJob);

            var existingJob = await scheduler.GetJobDetail(jobKey, ct).ConfigureAwait(false);
            var existingTrigger = await scheduler.GetTrigger(triggerKey, ct).ConfigureAwait(false);
            var existingJobTypeChanged = existingJob != null && existingJob.JobType != jobType;

            var jobDetail = JobBuilder.Create(jobType)
                .WithIdentity(jobKey)
                .WithDescription(handler.DisplayName)
                .UsingJobData(ScheduledHandlerJobBase.HandlerCodeKey, handler.HandlerCode)
                .StoreDurably()
                .Build();

            var trigger = BuildTrigger(triggerKey, jobKey, handler);

            if (existingJob == null || existingTrigger == null || existingJobTypeChanged)
            {
                await scheduler.ScheduleJob(jobDetail, new[] { trigger }, replace: true, ct).ConfigureAwait(false);
                _logger.LogInformation(
                    "Quartz job scheduled: code={Code}, schedule={ScheduleKind}, jobType={JobType}",
                    handler.HandlerCode,
                    schedule.Kind,
                    jobType.Name);
                return;
            }

            if (IsTriggerSameAs(existingTrigger, schedule))
            {
                // schedule 未变,保留 NEXT_FIRE_TIME / TIMES_TRIGGERED
                return;
            }

            await scheduler.AddJob(jobDetail, replace: true, storeNonDurableWhileAwaitingScheduling: true, ct).ConfigureAwait(false);
            await scheduler.RescheduleJob(triggerKey, trigger, ct).ConfigureAwait(false);
            _logger.LogInformation(
                "Quartz trigger rescheduled: code={Code}, schedule={ScheduleKind}",
                handler.HandlerCode,
                schedule.Kind);
        }

        private ITrigger BuildTrigger(TriggerKey triggerKey, JobKey jobKey, IScheduledHandler handler)
        {
            var schedule = handler.Schedule;
            var builder = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .ForJob(jobKey)
                .WithDescription(handler.DisplayName);

            builder = schedule.StartDelay > TimeSpan.Zero
                ? builder.StartAt(DateTimeOffset.UtcNow + schedule.StartDelay)
                : builder.StartNow();

            switch (schedule.Kind)
            {
                case ScheduleKind.FixedInterval:
                    builder = builder.WithSimpleSchedule(s => s
                        .WithInterval(schedule.Interval)
                        .RepeatForever()
                        .WithMisfireHandlingInstructionNextWithRemainingCount());
                    break;
                case ScheduleKind.Cron:
                    builder = builder.WithCronSchedule(schedule.CronExpression!, c =>
                    {
                        if (!string.IsNullOrWhiteSpace(schedule.TimeZoneId))
                            c.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId));
                        c.WithMisfireHandlingInstructionDoNothing();
                    });
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported ScheduleKind: {schedule.Kind}");
            }

            return builder.Build();
        }

        private static bool IsTriggerSameAs(ITrigger existing, ScheduleDescriptor desired)
        {
            return (existing, desired.Kind) switch
            {
                (ISimpleTrigger simple, ScheduleKind.FixedInterval) =>
                    simple.RepeatInterval == desired.Interval,
                (ICronTrigger cron, ScheduleKind.Cron) =>
                    string.Equals(cron.CronExpressionString, desired.CronExpression, StringComparison.Ordinal),
                _ => false,
            };
        }

        private async Task CleanupOrphansAsync(
            IScheduler scheduler,
            IReadOnlyCollection<IScheduledHandler> handlers,
            CancellationToken ct)
        {
            var aliveCodes = new HashSet<string>(
                handlers.Select(h => h.HandlerCode),
                StringComparer.OrdinalIgnoreCase);

            var existing = await scheduler
                .GetJobKeys(GroupMatcher<JobKey>.GroupEquals(_quartzOptions.JobGroup), ct)
                .ConfigureAwait(false);

            foreach (var key in existing)
            {
                if (aliveCodes.Contains(key.Name)) continue;

                await scheduler.DeleteJob(key, ct).ConfigureAwait(false);
                _logger.LogInformation(
                    "Quartz orphan job removed (no matching handler in registry): {Code}",
                    key.Name);
            }
        }
    }
}
