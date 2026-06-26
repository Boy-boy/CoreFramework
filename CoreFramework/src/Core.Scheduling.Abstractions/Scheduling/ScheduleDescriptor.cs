using System;

namespace Core.Scheduling
{
    /// <summary>
    /// 调度描述符:handler 自带的触发节奏声明。
    /// BG 直接用 <see cref="Interval"/> 计算下次触发;Quartz 翻译成 SimpleTrigger 或 CronTrigger;
    /// Hangfire 翻译成 RecurringJob 的 cron 表达式。
    /// </summary>
    public sealed class ScheduleDescriptor
    {
        private ScheduleDescriptor(
            ScheduleKind kind,
            TimeSpan interval,
            string cronExpression,
            string timeZoneId,
            TimeSpan startDelay,
            bool allowConcurrentExecution,
            TimeSpan? maxBackoff)
        {
            Kind = kind;
            Interval = interval;
            CronExpression = cronExpression;
            TimeZoneId = timeZoneId;
            StartDelay = startDelay;
            AllowConcurrentExecution = allowConcurrentExecution;
            MaxBackoff = maxBackoff;
        }

        /// <summary>触发节奏类型。</summary>
        public ScheduleKind Kind { get; }

        /// <summary>固定间隔(Cron 模式下为 <see cref="TimeSpan.Zero"/>)。</summary>
        public TimeSpan Interval { get; }

        /// <summary>
        /// Cron 表达式;固定间隔模式为 <see langword="null"/>。
        /// 因适配器而异:Quartz 6/7 字段(秒级);Hangfire(Cronos)5 字段(分钟级)或 6 字段(秒级)。
        /// 跨适配器移植时注意字段数差异。
        /// </summary>
        public string CronExpression { get; }

        /// <summary>Cron 时区 ID(IANA 或 Windows);<see langword="null"/> 表示系统默认时区。</summary>
        public string TimeZoneId { get; }

        /// <summary>首次触发前的等待时长(默认 <see cref="TimeSpan.Zero"/>)。</summary>
        public TimeSpan StartDelay { get; }

        /// <summary>
        /// 是否允许并发执行(默认 false)。
        /// BG 跳过本轮;Quartz 启用 <c>DisallowConcurrentExecution</c>;Hangfire 用带 <c>DisableConcurrentExecution</c> 的 sequential 入口。
        /// </summary>
        public bool AllowConcurrentExecution { get; }

        /// <summary>连续失败时的最大退避间隔;<see langword="null"/> 走 BackgroundSchedulingOptions 全局默认。</summary>
        public TimeSpan? MaxBackoff { get; }

        /// <summary>构造固定间隔描述符。</summary>
        /// <param name="interval">触发间隔,必须 &gt; 0。</param>
        /// <param name="startDelay">首次触发前等待时长。</param>
        /// <param name="allowConcurrentExecution">是否允许并发执行。</param>
        /// <param name="maxBackoff">最大退避间隔。</param>
        public static ScheduleDescriptor FixedInterval(
            TimeSpan interval,
            TimeSpan? startDelay = null,
            bool allowConcurrentExecution = false,
            TimeSpan? maxBackoff = null)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be positive.");

            return new ScheduleDescriptor(
                ScheduleKind.FixedInterval,
                interval,
                cronExpression: null,
                timeZoneId: null,
                startDelay: startDelay ?? TimeSpan.Zero,
                allowConcurrentExecution: allowConcurrentExecution,
                maxBackoff: maxBackoff);
        }

        /// <summary>构造 Cron 描述符(Quartz / Hangfire 适配器支持,BG 不支持)。</summary>
        /// <param name="cronExpression">Cron 表达式;方言随适配器,见 <see cref="CronExpression"/> 说明。</param>
        /// <param name="timeZoneId">时区 ID;<see langword="null"/> 使用系统默认。</param>
        /// <param name="startDelay">首次触发前等待时长。</param>
        /// <param name="allowConcurrentExecution">是否允许并发执行。</param>
        /// <param name="maxBackoff">最大退避间隔。</param>
        public static ScheduleDescriptor Cron(
            string cronExpression,
            string timeZoneId = null,
            TimeSpan? startDelay = null,
            bool allowConcurrentExecution = false,
            TimeSpan? maxBackoff = null)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                throw new ArgumentException("Cron expression must be non-empty.", nameof(cronExpression));

            return new ScheduleDescriptor(
                ScheduleKind.Cron,
                interval: TimeSpan.Zero,
                cronExpression: cronExpression,
                timeZoneId: timeZoneId,
                startDelay: startDelay ?? TimeSpan.Zero,
                allowConcurrentExecution: allowConcurrentExecution,
                maxBackoff: maxBackoff);
        }
    }
}
