using System;

namespace Core.Scheduling.Options
{
    /// <summary>
    /// BG 调度宿主专属配置:主循环轮询、停机等待、退避兜底、分布式锁参数。
    /// 跨适配器共享的 filter 开关请见 <see cref="SchedulingFilterOptions"/>(独立的另一个 Options)。
    /// </summary>
    /// <remarks>
    /// 仅 <c>AddSchedulingBackground</c> 接受本类型。下列字段在 Hangfire / Quartz 下无意义:
    /// Hangfire 用自家 SchedulePollingInterval + 存储层锁;Quartz 用 WaitForJobsToComplete + QRTZ_LOCKS。
    /// </remarks>
    public sealed class BackgroundSchedulingOptions
    {
        /// <summary>
        /// 全局默认最大退避间隔。
        /// handler 自带的 <see cref="Core.Scheduling.ScheduleDescriptor.MaxBackoff"/> 优先;为 null 时用此值兜底。
        /// </summary>
        public TimeSpan DefaultMaxBackoff { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>主循环空闲轮询间隔:多久检查一次"现在该跑哪个 handler"。</summary>
        public TimeSpan IdleDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>优雅停机时等待 in-flight handler 完成的最长时间。</summary>
        public TimeSpan ShutdownGraceTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 分布式锁租约时长(默认 30s)。仅 BG + 真实锁实现(如 Redis)生效;noop 实现忽略此值。
        /// 持锁期间会按 <see cref="DistributedLockRenewalFraction"/> 自动续租
        /// (除非 <see cref="EnableDistributedLockRenewal"/>=false),设短一点更安全——崩溃时锁能更快释放。
        /// </summary>
        public TimeSpan DistributedLockLeaseDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 持锁期间自动续租(默认开启)。可在以下场景关掉:
        /// (1) 单节点 noop 锁——续租空操作仍起 Task,关了省分配;
        /// (2) 执行时间 &lt;&lt; 租约,续租触发不到,关了省 CPU。
        /// </summary>
        public bool EnableDistributedLockRenewal { get; set; } = true;

        /// <summary>
        /// 续租间隔占租约的比例(默认 0.5,即每 Lease/2 续一次)。
        /// 越小越频繁、越安全,Redis 压力也越大。范围 (0, 1)。
        /// </summary>
        public double DistributedLockRenewalFraction { get; set; } = 0.5;
    }
}
