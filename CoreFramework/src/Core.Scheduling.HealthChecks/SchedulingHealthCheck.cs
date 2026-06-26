using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Core.Scheduling.HealthChecks
{
    /// <summary>
    /// 调度健康检查:聚合 <see cref="IHandlerExecutionInspector"/> 中所有 handler 的状态,
    /// 按 <see cref="SchedulingHealthCheckOptions"/> 的三档阈值得出最终健康度。默认名:<c>scheduling</c>。
    /// </summary>
    public sealed class SchedulingHealthCheck : IHealthCheck
    {
        private readonly IHandlerExecutionInspector _inspector;
        private readonly IOptionsMonitor<SchedulingHealthCheckOptions> _optionsMonitor;
        private readonly TimeProvider _timeProvider;

        /// <summary>初始化健康检查。</summary>
        public SchedulingHealthCheck(
            IHandlerExecutionInspector inspector,
            IOptionsMonitor<SchedulingHealthCheckOptions> optionsMonitor,
            TimeProvider timeProvider)
        {
            _inspector = inspector;
            _optionsMonitor = optionsMonitor;
            _timeProvider = timeProvider;
        }

        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var options = _optionsMonitor.CurrentValue;
            var now = _timeProvider.GetUtcNow();
            var states = _inspector.GetAllStates();

            if (states.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    description: "No scheduled handlers registered.",
                    data: BuildEmptyData()));
            }

            var participating = FilterStates(states, options);
            if (participating.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    description: "No handlers participate in health check (all excluded).",
                    data: BuildEmptyData()));
            }

            var worst = HealthStatus.Healthy;
            var reasons = new List<string>();
            foreach (var state in participating)
            {
                var (status, reason) = EvaluateOne(state, options, now);
                if (status > worst) worst = status;
                if (reason != null) reasons.Add(reason);
            }

            var data = options.IncludePerHandlerDetails
                ? BuildDetailedData(participating, now)
                : BuildSummaryData(participating);

            var description = reasons.Count == 0
                ? "All scheduled handlers healthy."
                : string.Join(" | ", reasons);

            return Task.FromResult(worst switch
            {
                HealthStatus.Unhealthy => HealthCheckResult.Unhealthy(description, data: data),
                HealthStatus.Degraded => HealthCheckResult.Degraded(description, data: data),
                _ => HealthCheckResult.Healthy(description, data),
            });
        }

        private static IReadOnlyList<HandlerState> FilterStates(
            IReadOnlyCollection<HandlerState> states,
            SchedulingHealthCheckOptions options)
        {
            IEnumerable<HandlerState> source = states;
            if (options.IncludedHandlerCodes.Count > 0)
                source = source.Where(s => options.IncludedHandlerCodes.Contains(s.HandlerCode));
            if (options.ExcludedHandlerCodes.Count > 0)
                source = source.Where(s => !options.ExcludedHandlerCodes.Contains(s.HandlerCode));
            return source.ToArray();
        }

        private static (HealthStatus Status, string? Reason) EvaluateOne(
            HandlerState state,
            SchedulingHealthCheckOptions options,
            DateTimeOffset now)
        {
            // 1. 连续失败 → Unhealthy(优先)
            if (options.UnhealthyAfterConsecutiveFailures is { } unhealthyAt
                && state.ConsecutiveFailureCount >= unhealthyAt)
            {
                return (HealthStatus.Unhealthy,
                    $"{state.HandlerCode}: {state.ConsecutiveFailureCount} consecutive failures (>= {unhealthyAt}).");
            }

            // 2. 卡死:开始后超过阈值还没结束
            if (options.RunningThresholdForDegraded is { } runningThreshold
                && state.IsRunning
                && state.LastStartTime is { } start
                && now - start > runningThreshold)
            {
                return (HealthStatus.Degraded,
                    $"{state.HandlerCode}: running for {now - start} (> {runningThreshold}).");
            }

            // 3. 陈旧:好久没完成过(仅 BG 模式建议启用,集群下会误报)
            if (options.StaleThresholdForDegraded is { } staleThreshold
                && state.LastFinishTime is { } finish
                && now - finish > staleThreshold)
            {
                return (HealthStatus.Degraded,
                    $"{state.HandlerCode}: stale, last finish {now - finish} ago (> {staleThreshold}).");
            }

            // 4. 中等连续失败 → Degraded
            if (options.DegradedAfterConsecutiveFailures is { } degradedAt
                && state.ConsecutiveFailureCount >= degradedAt)
            {
                return (HealthStatus.Degraded,
                    $"{state.HandlerCode}: {state.ConsecutiveFailureCount} consecutive failures (>= {degradedAt}).");
            }

            return (HealthStatus.Healthy, null);
        }

        private static IReadOnlyDictionary<string, object> BuildEmptyData()
            => new Dictionary<string, object> { ["handlerCount"] = 0 };

        private static IReadOnlyDictionary<string, object> BuildSummaryData(IReadOnlyList<HandlerState> states)
        {
            return new Dictionary<string, object>
            {
                ["handlerCount"] = states.Count,
                ["runningCount"] = states.Count(s => s.IsRunning),
                ["totalConsecutiveFailures"] = states.Sum(s => s.ConsecutiveFailureCount),
            };
        }

        private static IReadOnlyDictionary<string, object> BuildDetailedData(
            IReadOnlyList<HandlerState> states,
            DateTimeOffset now)
        {
            var data = new Dictionary<string, object>
            {
                ["handlerCount"] = states.Count,
                ["runningCount"] = states.Count(s => s.IsRunning),
                ["checkedAt"] = now,
            };

            foreach (var state in states)
            {
                data[$"handler:{state.HandlerCode}"] = new
                {
                    isRunning = state.IsRunning,
                    consecutiveFailures = state.ConsecutiveFailureCount,
                    lastStatus = state.LastStatus?.ToString(),
                    lastStart = state.LastStartTime,
                    lastFinish = state.LastFinishTime,
                    lastSuccess = state.LastSuccessTime,
                    nextRun = state.NextRunTime,
                    lastError = state.LastError,
                };
            }

            return data;
        }
    }
}
