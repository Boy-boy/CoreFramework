using System;

namespace Core.Scheduling.Hangfire.Hosting
{
    /// <summary>
    /// Hangfire 适配器配置:只承载 Hangfire / 存储 / 服务器相关参数。
    /// 跨适配器共享的 filter 开关在 <see cref="Core.Scheduling.Hosting.SchedulingFilterOptions"/>;
    /// BG 专属字段(IdleDelay、退避等)在 Hangfire 模式下不读取也不绑定。
    /// </summary>
    public sealed class HangfireSchedulingOptions
    {
        /// <summary>存储模式。</summary>
        public HangfirePersistenceMode PersistenceMode { get; set; } = HangfirePersistenceMode.InMemory;

        /// <summary>
        /// 持久化数据库连接串。<see cref="HangfirePersistenceMode.SqlServer"/> 必填。
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// SqlServer 存储的 schema 名;默认 <c>HangFire</c>(Hangfire 默认值)。
        /// </summary>
        public string SqlServerSchemaName { get; set; } = "HangFire";

        /// <summary>
        /// 是否允许 Hangfire 在首次启动时自动创建/升级表结构。
        /// 生产环境多数希望关掉,先手工建表再上线 —— 默认 <see langword="true"/>(便利优先,与 Hangfire 默认一致)。
        /// </summary>
        public bool PrepareSchemaIfNecessary { get; set; } = true;

        /// <summary>
        /// Hangfire 调度器轮询间隔。决定 RecurringJob 多久检查一次"该不该触发"。
        /// 最小 1 秒;默认 15 秒(Hangfire 默认)。
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// RecurringJob ID 前缀,bootstrap 用 <c>{Prefix}{HandlerCode}</c> 作为 ID。
        /// 多套应用共用同一 Hangfire 存储时,用前缀互相隔离。
        /// </summary>
        public string JobIdPrefix { get; set; } = "core-scheduling:";

        /// <summary>
        /// 启动期是否清理"前缀匹配但注册表里不存在"的孤儿 RecurringJob。
        /// 多版本灰度并行时建议关掉。默认 <see langword="true"/>。
        /// </summary>
        public bool CleanupOrphanJobs { get; set; } = true;

        /// <summary>
        /// 非并发执行的 lock 持有时长(秒)。即 <see cref="global::Hangfire.DisableConcurrentExecutionAttribute"/>
        /// 的 <c>timeoutInSeconds</c>。建议设置为 handler 的最长可能执行时间。默认 5 分钟。
        /// </summary>
        public int NonConcurrentLockTimeoutSeconds { get; set; } = 5 * 60;

        /// <summary>
        /// Hangfire 后台服务器 worker 数(本节点并发处理 job 上限)。
        /// 默认 <c>Environment.ProcessorCount * 5</c>(Hangfire 默认)。
        /// </summary>
        public int? WorkerCount { get; set; }

        /// <summary>
        /// 服务器名(集群中本节点的可读标识)。<see langword="null"/> 由 Hangfire 自己生成 (machine 名 + GUID)。
        /// </summary>
        public string ServerName { get; set; }
    }

    /// <summary>
    /// Hangfire 存储模式。
    /// </summary>
    public enum HangfirePersistenceMode
    {
        /// <summary>内存版,不集群。仅供开发/单机使用。</summary>
        InMemory = 0,

        /// <summary>SqlServer 存储,集群可用。</summary>
        SqlServer = 1
    }
}
