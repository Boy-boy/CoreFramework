using System;

namespace Core.EventBus.Outbox
{
    /// <summary>Outbox dispatcher 的运行参数;通过 <c>configureOutbox</c> 回调或 <c>IConfiguration</c> 节点配置。</summary>
    /// <remarks>
    /// 权衡:<see cref="PollInterval"/> 短则及时但空轮询噪音多;<see cref="BatchSize"/> 大则吞吐高但单事务回滚代价大;
    /// <see cref="MaxRetries"/> 大则对临时故障宽容但坏消息堆积久。
    /// </remarks>
    public class OutboxOptions
    {
        /// <summary>dispatcher 主循环空闲时的轮询间隔(默认 2 秒);本轮有消息时立即拉下一批不等待。</summary>
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>单轮 <see cref="IOutboxStorage.FetchReadyAsync"/> 的最大数量(默认 100);整批共享一个 UoW,中途异常整批回滚。</summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>单条消息最大失败次数(含首次,默认 8),超过后移入死信表。</summary>
        public int MaxRetries { get; set; } = 8;

        /// <summary>指数退避初始等待时间(默认 5 秒);第 n 次失败约等 <c>InitialBackoff * 2^(n-1)</c>。</summary>
        public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>指数退避上限(默认 10 分钟),避免单条消息等待过久。</summary>
        public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>启动时自动调用 <see cref="IOutboxStorage.InitializeAsync"/>;默认 true,用 EF Migrations 时可关。</summary>
        public bool AutoInitialize { get; set; } = true;
    }
}
