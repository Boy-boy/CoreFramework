using System;

namespace Core.EventBus.Outbox
{
    /// <summary>
    /// 指数退避 + 抖动：把投递失败的消息均匀地推到未来时刻，避免"群体重试"放大故障。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 算法：<c>delay = min(initial * 2^(retry-1), max) ± 10% 抖动</c>。
    /// </para>
    /// <para>举例（默认 initial=5s, max=10min）：</para>
    /// <list type="bullet">
    ///   <item><description>1 次失败：约 5 秒后重试</description></item>
    ///   <item><description>2 次失败：约 10 秒</description></item>
    ///   <item><description>3 次失败：约 20 秒</description></item>
    ///   <item><description>7 次失败：约 320 秒</description></item>
    ///   <item><description>8 次及以后：被 max 截断为 600 秒</description></item>
    /// </list>
    /// <para>
    /// 抖动来源：由 <c>retryCount * 2654435761u ^ nowUtc.Ticks</c> 派生伪随机数，
    /// 不依赖 <c>Random</c> / <c>Date.Now</c>，保持可观察性的同时让同一时刻同一类型的失败
    /// 消息错峰重试，避免雪崩。
    /// </para>
    /// </remarks>
    public static class OutboxBackoff
    {
        /// <summary>
        /// 根据失败次数计算下一次允许投递的 UTC 时刻。
        /// </summary>
        /// <param name="retryCount"><b>本次失败之后</b>的累计失败次数（&gt;0）。</param>
        /// <param name="options">含 <see cref="OutboxOptions.InitialBackoff"/> / <see cref="OutboxOptions.MaxBackoff"/>。</param>
        /// <param name="nowUtc">当前 UTC 时刻，作为退避基准点。</param>
        /// <returns>"等到这个时刻才允许下一次投递"的 UTC 时刻；retryCount&le;0 时返回 nowUtc。</returns>
        public static DateTime CalculateNextRetry(int retryCount, OutboxOptions options, DateTime nowUtc)
        {
            if (retryCount <= 0) return nowUtc;

            var initialMs = options.InitialBackoff.TotalMilliseconds;
            var maxMs = options.MaxBackoff.TotalMilliseconds;

            // 2^(retry-1) 增长，retryCount 过大时 Math.Pow 会溢出 double，截断到 30 步
            var shift = Math.Min(retryCount - 1, 30);
            var raw = initialMs * Math.Pow(2, shift);
            var capped = Math.Min(raw, maxMs);

            // ±10% 抖动：避免一批同时失败的消息在同一时刻一起重试
            var jitter = (capped * 0.1) * (HashJitter(retryCount, nowUtc) - 0.5) * 2.0;
            var finalMs = Math.Max(0, capped + jitter);

            return nowUtc.AddMilliseconds(finalMs);
        }

        /// <summary>
        /// 由 retry 次数和当前时间派生 [0,1) 的伪随机数。不引入 Random 保证可重放。
        /// </summary>
        private static double HashJitter(int retryCount, DateTime nowUtc)
        {
            unchecked
            {
                // Knuth multiplicative hash constant
                var hash = (uint)(retryCount * 2654435761u ^ (uint)nowUtc.Ticks);
                return (hash & 0xFFFF) / (double)0x10000;
            }
        }
    }
}
