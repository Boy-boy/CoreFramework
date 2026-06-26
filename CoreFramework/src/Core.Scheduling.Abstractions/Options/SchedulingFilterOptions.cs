namespace Core.Scheduling.Options
{
    /// <summary>
    /// 跨适配器(BG / Hangfire / Quartz)共享的内置 filter 开关:Tracing / Metrics / Logging。
    /// 三种宿主下行为对称,各自 Options 实例独立绑定与热更新。
    /// </summary>
    public sealed class SchedulingFilterOptions
    {
        /// <summary>启用 Tracing 过滤器(OpenTelemetry / Activity)。</summary>
        public bool EnableTracing { get; set; } = true;

        /// <summary>启用 Metrics 过滤器。</summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>启用 Logging 过滤器。</summary>
        public bool EnableLogging { get; set; } = true;
    }
}
