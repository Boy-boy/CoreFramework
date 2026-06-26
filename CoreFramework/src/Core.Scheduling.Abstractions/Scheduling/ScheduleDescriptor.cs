using System;

namespace Core.Scheduling
{
    /// <summary>
    /// 调度描述符：每个 <see cref="IScheduledHandler"/> 自带的触发节奏声明。
    /// 同一份描述符被默认 BG 适配器和 Quartz 适配器分别解析：
    /// BG 用 <see cref="Interval"/> 直接计算下次触发；Quartz 翻译成 SimpleTrigger 或 CronTrigger。
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

        /// <summary>
        /// 触发节奏类型。
        /// </summary>
        public ScheduleKind Kind { get; }

        /// <summary>
        /// 固定间隔模式下的触发间隔；Cron 模式下为 <see cref="TimeSpan.Zero"/>。
        /// </summary>
        public TimeSpan Interval { get; }

        /// <summary>
        /// Cron 表达式；固定间隔模式下为 <see langword="null"/>。
        /// 表达式格式遵循 Quartz Cron（六/七字段，秒级）规范。
        /// </summary>
        public string CronExpression { get; }

        /// <summary>
        /// Cron 解析使用的时区 ID（IANA 或 Windows）；<see langword="null"/> 表示使用系统默认时区。
        /// </summary>
        public string TimeZoneId { get; }

        /// <summary>
        /// 首次触发前的等待时长；默认 <see cref="TimeSpan.Zero"/> 表示部署/首次注册后立刻进入下一轮判定。
        /// </summary>
        public TimeSpan StartDelay { get; }

        /// <summary>
        /// 是否允许同一 handler 同时存在多次执行。
        /// 默认 <see langword="false"/>：BG 跳过本轮；Quartz 启用 <c>DisallowConcurrentExecution</c>。
        /// </summary>
        public bool AllowConcurrentExecution { get; }

        /// <summary>
        /// 连续失败时的最大退避间隔；<see langword="null"/> 表示由 BackgroundSchedulingOptions 全局默认值兜底。
        /// </summary>
        public TimeSpan? MaxBackoff { get; }

        /// <summary>
        /// 构造一个固定间隔触发描述符。
        /// </summary>
        /// <param name="interval">基础触发间隔，必须大于零。</param>
        /// <param name="startDelay">首次触发前的等待时长，默认 <see cref="TimeSpan.Zero"/>。</param>
        /// <param name="allowConcurrentExecution">是否允许同一 handler 并发执行，默认 <see langword="false"/>。</param>
        /// <param name="maxBackoff">连续失败时的最大退避间隔；<see langword="null"/> 走全局默认。</param>
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

        /// <summary>
        /// 构造一个 Cron 触发描述符。注意：默认 BG 适配器不支持 Cron，仅在引入 Quartz 适配器时可用。
        /// </summary>
        /// <param name="cronExpression">Quartz 风格 Cron 表达式（秒级，六/七字段）。</param>
        /// <param name="timeZoneId">解析使用的时区 ID；<see langword="null"/> 使用系统默认。</param>
        /// <param name="startDelay">首次触发前的等待时长，默认 <see cref="TimeSpan.Zero"/>。</param>
        /// <param name="allowConcurrentExecution">是否允许同一 handler 并发执行，默认 <see langword="false"/>。</param>
        /// <param name="maxBackoff">连续失败时的最大退避间隔；<see langword="null"/> 走全局默认。</param>
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
