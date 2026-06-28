using System;

namespace Core.EventBus.Outbox
{
    /// <summary>跑死信表自动清理时的保留期等参数,默认值见各属性 doc。</summary>
    /// <remarks>
    /// 与 <see cref="OutboxOptions"/> 拆开是因为它们在生命周期上独立 — outbox 与死信清理两个 BackgroundService 各自配置。
    /// </remarks>
    public class DeadLetterCleanupOptions
    {
        /// <summary>死信记录保留天数(默认 90)。早于阈值的死信被自动删除。</summary>
        public int RetentionDays { get; set; } = 90;

        /// <summary>启动时自动建表(<c>EnsureCreated</c>);默认 true,用 EF Migrations 时可关。</summary>
        public bool AutoInitialize { get; set; } = true;

        /// <summary>两次清理之间的间隔,默认 1 天。</summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);

        /// <summary>启动期校验,数值非法立刻抛 <see cref="InvalidOperationException"/>。</summary>
        public void Validate()
        {
            if (RetentionDays <= 0)
                throw new InvalidOperationException(
                    $"{nameof(DeadLetterCleanupOptions)}.{nameof(RetentionDays)} 必须 > 0,当前={RetentionDays}。");
            if (CleanupInterval <= TimeSpan.Zero)
                throw new InvalidOperationException(
                    $"{nameof(DeadLetterCleanupOptions)}.{nameof(CleanupInterval)} 必须 > 0,当前={CleanupInterval}。");
        }
    }

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

        /// <summary>启动期校验,数值非法立刻抛 <see cref="InvalidOperationException"/>。</summary>
        /// <remarks>避免运行时静默退化(<c>BatchSize=0</c> dispatcher 永远拉空、<c>MaxRetries&lt;0</c> 所有失败立刻进死信)。</remarks>
        public void Validate()
        {
            if (PollInterval <= TimeSpan.Zero)
                throw new InvalidOperationException(
                    $"{nameof(OutboxOptions)}.{nameof(PollInterval)} 必须 > 0,当前={PollInterval}(否则 dispatcher 会 CPU 满载空轮询)。");
            if (BatchSize <= 0)
                throw new InvalidOperationException(
                    $"{nameof(OutboxOptions)}.{nameof(BatchSize)} 必须 > 0,当前={BatchSize}。");
            if (MaxRetries < 0)
                throw new InvalidOperationException(
                    $"{nameof(OutboxOptions)}.{nameof(MaxRetries)} 不能为负数,当前={MaxRetries}(负数会让所有失败立刻进死信)。");
            if (InitialBackoff <= TimeSpan.Zero)
                throw new InvalidOperationException(
                    $"{nameof(OutboxOptions)}.{nameof(InitialBackoff)} 必须 > 0,当前={InitialBackoff}。");
            if (MaxBackoff < InitialBackoff)
                throw new InvalidOperationException(
                    $"{nameof(OutboxOptions)}.{nameof(MaxBackoff)} 不能小于 {nameof(InitialBackoff)},当前={MaxBackoff} vs {InitialBackoff}。");
        }
    }
}
