using System;
using System.Collections.Generic;

namespace Core.Scheduling.Models
{
    /// <summary>一次触发的执行上下文(由运行时构造)。</summary>
    public sealed class HandlerExecutionContext
    {
        /// <summary>触发的 handler 编码。</summary>
        public string HandlerCode { get; }

        /// <summary>计划触发时间(BG: 上次触发 + Interval; Quartz: ScheduledFireTime)。</summary>
        public DateTimeOffset ScheduledTime { get; }

        /// <summary>实际开始时间,与 <see cref="ScheduledTime"/> 的差值即漂移。</summary>
        public DateTimeOffset FireTime { get; }

        /// <summary>本次触发的 DI 作用域,filter / handler 应通过它解析 scoped 服务。</summary>
        public IServiceProvider Services { get; }

        /// <summary>扩展槽,用于在 filter 之间或 filter→handler 传递跨切面数据。</summary>
        public IDictionary<string, object> Items { get; }

        /// <summary>初始化触发上下文(运行时使用)。</summary>
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
            Items = new Dictionary<string, object>();
        }
    }
}
