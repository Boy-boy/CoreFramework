namespace Core.Scheduling.Hosting
{
    /// <summary>
    /// 跨适配器（BG / Hangfire / Quartz）共享的内置 filter 开关。
    /// 仅承载 Tracing / Metrics / Logging 三个开关——这些是框架默认提供、
    /// 与具体宿主无关的可观测性 filter,在三种宿主下行为一致。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 三种宿主的注册扩展(<c>AddSchedulingBackground</c> / <c>AddSchedulingHangfire</c> /
    /// <c>AddSchedulingQuartz</c>)都暴露本类型作为 filter 配置入口,行为对称。
    /// </para>
    /// <para>
    /// 内部 filter 直接注入 <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/>
    /// of <see cref="SchedulingFilterOptions"/>,跟 BG 专属的 <see cref="SchedulingOptions"/> 完全解耦——
    /// 两者是独立 Options 实例,各自独立绑定 / 热更新。
    /// </para>
    /// </remarks>
    public sealed class SchedulingFilterOptions
    {
        /// <summary>
        /// 是否启用内置 Tracing 过滤器（OpenTelemetry/Activity）。
        /// </summary>
        public bool EnableTracing { get; set; } = true;

        /// <summary>
        /// 是否启用内置 Metrics 过滤器。
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// 是否启用内置 Logging 过滤器。
        /// </summary>
        public bool EnableLogging { get; set; } = true;
    }
}
