namespace Core.Scheduling
{
    /// <summary>
    /// 触发节奏类型。
    /// </summary>
    public enum ScheduleKind
    {
        /// <summary>
        /// 固定间隔：每隔 <see cref="ScheduleDescriptor.Interval"/> 触发一次。BG 与 Quartz 适配器均支持。
        /// </summary>
        FixedInterval = 1,

        /// <summary>
        /// Cron 表达式：仅 Quartz 适配器支持；BG 适配器在启动时会抛出异常并指引切换。
        /// </summary>
        Cron = 2
    }
}
