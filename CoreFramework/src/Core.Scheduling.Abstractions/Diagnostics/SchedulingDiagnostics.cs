namespace Core.Scheduling.Diagnostics
{
    /// <summary>可观测性常量,用于对接 OpenTelemetry / Prometheus / 自家观测体系。</summary>
    public static class SchedulingDiagnostics
    {
        /// <summary>Meter 名,订阅即可拿到内置 Metrics。</summary>
        public const string MeterName = "Core.Scheduling";

        /// <summary>ActivitySource 名,订阅即可拿到分布式追踪 span。</summary>
        public const string ActivitySourceName = "Core.Scheduling";

        /// <summary>执行总次数计数器(按 status 维度)。</summary>
        public const string MetricExecutionCount = "scheduling.executions.count";

        /// <summary>执行耗时直方图(毫秒)。</summary>
        public const string MetricExecutionDuration = "scheduling.executions.duration";

        /// <summary>连续失败次数仪表(按 handler 维度)。</summary>
        public const string MetricConsecutiveFailures = "scheduling.executions.consecutive_failures";

        /// <summary>Tag: handler 编码。</summary>
        public const string TagHandlerCode = "handler.code";

        /// <summary>Tag: 执行状态。</summary>
        public const string TagStatus = "status";
    }
}
