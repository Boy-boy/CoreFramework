using System;

namespace Core.EventBus.Outbox
{
    /// <summary>
    /// Outbox dispatcher 的运行参数。在 <c>options.AddEfCoreEventBusStorage&lt;TDbContext&gt;(...)</c> 时通过
    /// <c>configureOutbox</c> 回调或 <c>IConfiguration</c> 节点配置。
    /// </summary>
    /// <remarks>
    /// 参数选取的权衡：
    /// <list type="bullet">
    ///   <item><description><b>延迟 vs 资源</b>：<see cref="PollInterval"/> 越短 broker 接收越及时，但空轮询消耗 DB 连接 / 日志噪音越多。</description></item>
    ///   <item><description><b>吞吐 vs 长尾</b>：<see cref="BatchSize"/> 越大单轮吞吐越高，但 dispatcher 失败时单事务回滚的代价也越大。</description></item>
    ///   <item><description><b>韧性 vs 容忍丢失</b>：<see cref="MaxRetries"/> 越大对临时故障越宽容，但坏消息会在表里堆积更久。</description></item>
    /// </list>
    /// </remarks>
    public class OutboxOptions
    {
        /// <summary>
        /// dispatcher 主循环空闲时的轮询间隔；默认 2 秒。
        /// 当本轮有处理消息时不等待，立即拉下一批。
        /// </summary>
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// 每轮 <see cref="IOutboxStorage.FetchReadyAsync"/> 的最大数量；默认 100。
        /// 该批次内的消息共享同一个 dispatcher UoW —— 若中途 dispatcher 自身异常，整批回滚。
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// 单条消息允许的最大失败次数（含首次），超过后被移入死信表。
        /// 默认 8 次配合默认退避（5s → 10s → 20s → 40s → 80s → 160s → 320s → 600s）大约总等 ~20 分钟。
        /// </summary>
        public int MaxRetries { get; set; } = 8;

        /// <summary>
        /// 指数退避的初始等待时间；默认 5 秒。第 n 次失败的等待时间约为 <c>InitialBackoff * 2^(n-1)</c>。
        /// </summary>
        public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 指数退避的上限，避免单条消息等待过久；默认 10 分钟。
        /// </summary>
        public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 启动时自动调用 <see cref="IOutboxStorage.InitializeAsync"/>（典型为 <c>EnsureCreated</c> / <c>CREATE IF NOT EXISTS</c>）；
        /// 默认 true。如果你用 EF Migrations 管理 schema，可关掉避免重复建表的副作用。
        /// </summary>
        public bool AutoInitialize { get; set; } = true;
    }
}
