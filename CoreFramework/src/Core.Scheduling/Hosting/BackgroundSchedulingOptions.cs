using System;

namespace Core.Scheduling.Hosting
{
    /// <summary>
    /// 默认 BG 调度宿主专属配置:主循环轮询间隔、停机等待时长、退避兜底、分布式锁租约/续租参数。
    /// 跨适配器共享的 filter 开关(Tracing/Metrics/Logging)请见 <see cref="SchedulingFilterOptions"/>,
    /// 那是独立的另一个 Options 类型,本类不再继承它。
    /// </summary>
    /// <remarks>
    /// 仅 <c>AddSchedulingBackground</c>(BG) 的注册回调接受本类型。
    /// 下列字段在 Hangfire/Quartz 宿主下没有意义,因此在那两个适配器的 API 里都拿不到:
    /// <list type="bullet">
    /// <item>Hangfire 用自家 <c>SchedulePollingInterval</c> + 存储层分布式锁;</item>
    /// <item>Quartz 用 <c>WaitForJobsToComplete</c> + <c>QRTZ_LOCKS</c>。</item>
    /// </list>
    /// </remarks>
    public sealed class BackgroundSchedulingOptions
    {
        /// <summary>
        /// 全局默认最大退避间隔。handler 自带的
        /// <see cref="Core.Scheduling.ScheduleDescriptor.MaxBackoff"/> 优先；为 <see langword="null"/> 时使用此值兜底。
        /// 仅 BG 模式参与下次时间计算;Hangfire/Quartz 由各自引擎控制触发节奏,本值无效。
        /// </summary>
        public TimeSpan DefaultMaxBackoff { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 主循环的空闲轮询间隔（仅 BG 模式有效）。
        /// 决定多久去检查一次"现在该跑哪个 handler"。
        /// </summary>
        public TimeSpan IdleDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// 优雅停机时等待 in-flight handler 完成的最长时间。
        /// 仅 BG 模式有效；Quartz 模式由 AddQuartzHostedService 自己的 WaitForJobsToComplete 控制。
        /// </summary>
        public TimeSpan ShutdownGraceTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 分布式锁租约时长。默认 30 秒。
        /// <para>
        /// 仅 BG 模式 + <see cref="Core.Scheduling.Abstractions.IDistributedHandlerLock"/> 的真实实现(Redis 等)生效;
        /// 默认 noop 实现忽略此值;Hangfire/Quartz 不走本框架的分布式锁。
        /// </para>
        /// 持锁期间 host 会按 <see cref="DistributedLockRenewalFraction"/> 自动续租
        /// (除非 <see cref="EnableDistributedLockRenewal"/>=false),
        /// 因此租约设短一点是安全的——节点崩溃时锁也能更快释放。
        /// </summary>
        public TimeSpan DistributedLockLeaseDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 是否在持锁期间自动续租。默认 <see langword="true"/>。
        /// <para>
        /// 关掉的两个场景:
        /// <list type="bullet">
        /// <item>单节点 noop 锁——续租是空操作但仍会起 Task,关了可省 Task 分配;</item>
        /// <item>handler 执行时间 &lt;&lt; 租约,续租触发不到,关了可省点 CPU。</item>
        /// </list>
        /// </para>
        /// </summary>
        public bool EnableDistributedLockRenewal { get; set; } = true;

        /// <summary>
        /// 续租触发的间隔占租约时长的比例。默认 <c>0.5</c>(每 <see cref="DistributedLockLeaseDuration"/>/2 续一次)。
        /// 越小越频繁、越安全,Redis 压力也越大。范围 (0, 1)。
        /// </summary>
        public double DistributedLockRenewalFraction { get; set; } = 0.5;
    }
}
