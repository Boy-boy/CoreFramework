using System;
using System.Collections.Generic;

namespace Core.Scheduling.HealthChecks
{
    /// <summary>
    /// 调度健康检查阈值。
    /// 检查项之间是"任一触发即生效"的关系，最严重者胜出（Unhealthy &gt; Degraded &gt; Healthy）。
    /// </summary>
    public sealed class SchedulingHealthCheckOptions
    {
        /// <summary>
        /// 连续失败次数 ≥ 此值 → Unhealthy。
        /// 设为 <see langword="null"/> 关闭此项判定。
        /// 默认 5。
        /// </summary>
        public int? UnhealthyAfterConsecutiveFailures { get; set; } = 5;

        /// <summary>
        /// 连续失败次数 ≥ 此值（但 &lt; <see cref="UnhealthyAfterConsecutiveFailures"/>） → Degraded。
        /// 设为 <see langword="null"/> 关闭此项判定。
        /// 默认 2。
        /// </summary>
        public int? DegradedAfterConsecutiveFailures { get; set; } = 2;

        /// <summary>
        /// 某 handler 标记为 Running 但开始执行后超过此时长 → Degraded（视为卡死）。
        /// 设为 <see langword="null"/> 关闭此项判定。
        /// 默认 5 分钟。
        /// </summary>
        public TimeSpan? RunningThresholdForDegraded { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 某 handler 距离上次完成时间已超过此时长 → Degraded（视为陈旧）。
        /// <para>
        /// <b>注意</b>:Quartz 集群模式下,本节点 inspector 只反映"本节点最近一次执行";
        /// 别的节点抢到的触发不会更新本节点状态。
        /// 此项在集群下会误报,因此**默认关闭**(null);仅 BG 单机模式建议开启。
        /// </para>
        /// </summary>
        public TimeSpan? StaleThresholdForDegraded { get; set; }

        /// <summary>
        /// 白名单。非空时只有列出的 HandlerCode 参与判定;为空则全部参与。
        /// </summary>
        public HashSet<string> IncludedHandlerCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 黑名单。匹配的 HandlerCode 不参与判定。优先级高于白名单。
        /// </summary>
        public HashSet<string> ExcludedHandlerCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 是否在 <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Data"/>
        /// 里附带每个 handler 的快照,便于 /health 端点直接看到细节。
        /// 默认 <see langword="true"/>。
        /// </summary>
        public bool IncludePerHandlerDetails { get; set; } = true;
    }
}
