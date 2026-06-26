namespace Core.Scheduling
{
    /// <summary>触发节奏类型。</summary>
    public enum ScheduleKind
    {
        /// <summary>固定间隔触发,BG / Quartz 均支持。</summary>
        FixedInterval = 1,

        /// <summary>Cron 表达式触发,Quartz / Hangfire 支持;BG 不支持,启动时会抛异常并提示切换适配器。</summary>
        Cron = 2
    }
}
