namespace Core.Scheduling.Diagnostics
{
    /// <summary>
    /// 调度运行时使用的可观测性常量。
    /// 消费者用这些名字接 OpenTelemetry / Prometheus / 自家观测体系。
    /// </summary>
    public static class SchedulingDiagnostics
    {
        /// <summary>Metrics Meter 名。订阅它即可拿到所有内置指标。</summary>
        public const string MeterName = "Core.Scheduling";

        /// <summary>Activity Source 名。订阅它即可拿到分布式追踪 span。</summary>
        public const string ActivitySourceName = "Core.Scheduling";

        /// <summary>执行总次数计数器（按 status 维度）。</summary>
        public const string MetricExecutionCount = "scheduling.executions.count";

        /// <summary>执行耗时直方图（毫秒）。</summary>
        public const string MetricExecutionDuration = "scheduling.executions.duration";

        /// <summary>连续失败次数仪表（按 handler 维度，最新值）。</summary>
        public const string MetricConsecutiveFailures = "scheduling.executions.consecutive_failures";

        /// <summary>Tag 名：handler 编码。</summary>
        public const string TagHandlerCode = "handler.code";

        /// <summary>Tag 名：执行状态。</summary>
        public const string TagStatus = "status";
    }
}
