using System;

namespace Core.Scheduling.Quartz.Hosting
{
    /// <summary>
    /// Quartz 适配器配置。
    /// 通用调度参数（默认间隔、最大退避等）继续读 <see cref="Core.Scheduling.Hosting.SchedulingOptions"/>，
    /// 本对象只承载 Quartz/AdoJobStore/集群相关参数。
    /// </summary>
    public sealed class QuartzSchedulingOptions
    {
        /// <summary>
        /// 存储模式。
        /// </summary>
        public QuartzPersistenceMode PersistenceMode { get; set; } = QuartzPersistenceMode.InMemory;

        /// <summary>
        /// 持久化数据库连接串。<see cref="QuartzPersistenceMode.SqlServer"/> /
        /// <see cref="QuartzPersistenceMode.PostgreSql"/> 必填。
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>Quartz 调度器名,集群内各节点必须一致。</summary>
        public string SchedulerName { get; set; } = "Core.Scheduling.Scheduler";

        /// <summary>当前节点 ID,"AUTO" 由 Quartz 自动生成。</summary>
        public string InstanceId { get; set; } = "AUTO";

        /// <summary>AdoJobStore 表前缀,需与建表脚本一致。</summary>
        public string TablePrefix { get; set; } = "QRTZ_";

        /// <summary>
        /// 是否启用集群模式。仅在 <see cref="PersistenceMode"/> 非 <see cref="QuartzPersistenceMode.InMemory"/> 时有效。
        /// </summary>
        public bool ClusterEnabled { get; set; } = true;

        /// <summary>集群心跳间隔。</summary>
        public TimeSpan ClusterCheckinInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>本节点 Quartz 工作线程池大小。</summary>
        public int ThreadCount { get; set; } = 4;

        /// <summary>Job 分组,所有 handler 共用此分组。</summary>
        public string JobGroup { get; set; } = "core-scheduling";

        /// <summary>
        /// 启动期是否清理孤儿 Job（注册表里没有但 QRTZ_ 表里残留的）。
        /// 多版本灰度并行时建议关闭，避免互相清理对方的 Job。
        /// </summary>
        public bool CleanupOrphanJobs { get; set; } = true;
    }

    /// <summary>
    /// Quartz 持久化模式。
    /// </summary>
    public enum QuartzPersistenceMode
    {
        /// <summary>RAMJobStore,内存版,不集群。仅供开发/单机使用。</summary>
        InMemory = 0,

        /// <summary>SqlServer AdoJobStore,集群可用。</summary>
        SqlServer = 1,

        /// <summary>PostgreSQL AdoJobStore,集群可用。</summary>
        PostgreSql = 2
    }
}
