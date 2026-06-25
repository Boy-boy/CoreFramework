using System;
using System.Collections.Generic;

namespace Core.Scheduling.Models
{
    /// <summary>
    /// 一次触发的执行上下文。
    /// </summary>
    public sealed class HandlerExecutionContext
    {
        /// <summary>
        /// 触发当前 handler 的代码（即 <see cref="Core.Scheduling.Abstractions.IScheduledHandler.HandlerCode"/>）。
        /// </summary>
        public string HandlerCode { get; }

        /// <summary>
        /// 计划触发时间。BG 模式下等于上一次触发 + Interval；Quartz 模式下取 ITrigger 的 ScheduledFireTime。
        /// </summary>
        public DateTimeOffset ScheduledTime { get; }

        /// <summary>
        /// 实际开始触发时间。与 <see cref="ScheduledTime"/> 的差值即为漂移。
        /// </summary>
        public DateTimeOffset FireTime { get; }

        /// <summary>
        /// 本次触发所属的 DI 作用域提供者。filter / handler 应通过它解析 scoped 服务，避免捕获根容器。
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// 扩展槽，供 filter 之间或 filter→handler 传递跨切面数据。
        /// </summary>
        public IDictionary<string, object?> Items { get; }

        /// <summary>
        /// 初始化触发上下文。一般由运行时构造，业务侧不需要手动 new。
        /// </summary>
        public HandlerExecutionContext(
            string handlerCode,
            DateTimeOffset scheduledTime,
            DateTimeOffset fireTime,
            IServiceProvider services)
        {
            if (string.IsNullOrWhiteSpace(handlerCode))
                throw new ArgumentException("HandlerCode must be non-empty.", nameof(handlerCode));

            HandlerCode = handlerCode;
            ScheduledTime = scheduledTime;
            FireTime = fireTime;
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Items = new Dictionary<string, object?>();
        }
    }
}
