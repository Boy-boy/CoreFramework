using System;

namespace Core.EventBus.Outbox
{
    /// <summary>指数退避 + 抖动:把失败消息错峰推到未来,避免群体重试雪崩。</summary>
    /// <remarks>
    /// <c>delay = min(initial * 2^(retry-1), max) ± 10% 抖动</c>。
    /// 抖动从 <c>retryCount</c> + <c>nowUtc.Ticks</c> + <c>messageId</c> 派生,不依赖 <c>Random</c>,保持可观察性;
    /// 混入 messageId 解决"同时刻同 ticks 失败的不同消息抖动相同"的群聚问题。
    /// </remarks>
    public static class OutboxBackoff
    {
        /// <summary>根据失败次数计算下次允许投递的 UTC 时刻;retryCount &le; 0 时返回 nowUtc。</summary>
        /// <param name="retryCount">本次失败后的累计失败次数(&gt;0)。</param>
        /// <param name="options">含 <see cref="OutboxOptions.InitialBackoff"/> / <see cref="OutboxOptions.MaxBackoff"/>。</param>
        /// <param name="nowUtc">退避基准点(UTC)。</param>
        /// <param name="messageId">消息 Id,用于让同时刻不同消息得到不同的抖动量(避免群聚)。</param>
        public static DateTime CalculateNextRetry(int retryCount, OutboxOptions options, DateTime nowUtc, Guid messageId = default)
        {
            if (retryCount <= 0) return nowUtc;

            var initialMs = options.InitialBackoff.TotalMilliseconds;
            var maxMs = options.MaxBackoff.TotalMilliseconds;

            // 截断到 30 步,避免 Math.Pow 让 double 溢出
            var shift = Math.Min(retryCount - 1, 30);
            var raw = initialMs * Math.Pow(2, shift);
            var capped = Math.Min(raw, maxMs);

            // ±10% 抖动让一批同时失败的消息错峰
            var jitter = (capped * 0.1) * (HashJitter(retryCount, nowUtc, messageId) - 0.5) * 2.0;
            var finalMs = Math.Max(0, capped + jitter);

            return nowUtc.AddMilliseconds(finalMs);
        }

        /// <summary>由 retry 次数、当前时间、messageId 派生 [0,1) 伪随机数;不用 Random,便于复盘。</summary>
        private static double HashJitter(int retryCount, DateTime nowUtc, Guid messageId)
        {
            unchecked
            {
                // Knuth 乘法 hash 常量 + Guid.GetHashCode 把消息 Id 混入,
                // 让"同时刻 + 同 retry 次数"的不同消息分散到不同抖动量
                var hash = (uint)(retryCount * 2654435761u
                                  ^ (uint)nowUtc.Ticks
                                  ^ (uint)messageId.GetHashCode());
                return (hash & 0xFFFF) / (double)0x10000;
            }
        }
    }
}
