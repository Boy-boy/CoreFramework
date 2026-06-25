namespace Core.Scheduling.Hosting
{
    /// <summary>
    /// 跨适配器（BG / Hangfire / Quartz）共享的调度配置子集。
    /// 仅承载与具体宿主无关的字段:内置 filter 开关。
    /// BG 模式专属字段（主循环间隔、停机等待、分布式锁、退避兜底等）请见派生类 <see cref="SchedulingOptions"/>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hangfire 与 Quartz 适配器的注册回调只暴露本类型,
    /// 防止用户在那些模式下设置 <see cref="SchedulingOptions.IdleDelay"/> 之类的死字段而误以为生效。
    /// 内部 filter 注入的仍是 <see cref="SchedulingOptions"/>,
    /// 因为 <see cref="SchedulingOptions"/> 继承本类,Shared API 写入的值会落到同一实例上。
    /// </para>
    /// <para>
    /// 退避相关的 <see cref="SchedulingOptions.DefaultMaxBackoff"/> 只在 BG 模式生效:
    /// Hangfire/Quartz 的下次触发时间由各自引擎(RecurringJob/Trigger)决定,框架不重算。
    /// </para>
    /// </remarks>
    public class SharedSchedulingOptions
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
