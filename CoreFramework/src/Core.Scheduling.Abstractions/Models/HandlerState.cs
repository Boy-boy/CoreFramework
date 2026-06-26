using System;

namespace Core.Scheduling.Models
{
    /// <summary>处理器状态快照(不变对象),由 <see cref="Abstractions.IHandlerExecutionInspector"/> 对外暴露只读视图。</summary>
    public sealed class HandlerState
    {
        /// <summary>初始化状态快照。</summary>
        public HandlerState(
            string handlerCode,
            bool isRunning,
            DateTimeOffset? lastStartTime,
            DateTimeOffset? lastFinishTime,
            DateTimeOffset? lastSuccessTime,
            DateTimeOffset? nextRunTime,
            int consecutiveFailureCount,
            string lastError,
            HandlerExecutionStatus? lastStatus)
        {
            HandlerCode = handlerCode;
            IsRunning = isRunning;
            LastStartTime = lastStartTime;
            LastFinishTime = lastFinishTime;
            LastSuccessTime = lastSuccessTime;
            NextRunTime = nextRunTime;
            ConsecutiveFailureCount = consecutiveFailureCount;
            LastError = lastError;
            LastStatus = lastStatus;
        }

        /// <summary>处理器编码。</summary>
        public string HandlerCode { get; }

        /// <summary>是否正在执行(本节点视角)。</summary>
        public bool IsRunning { get; }

        /// <summary>上次开始时间。</summary>
        public DateTimeOffset? LastStartTime { get; }

        /// <summary>上次完成时间(无论成败)。</summary>
        public DateTimeOffset? LastFinishTime { get; }

        /// <summary>上次成功时间。</summary>
        public DateTimeOffset? LastSuccessTime { get; }

        /// <summary>下次预计触发时间(仅 BG 模式可信;Quartz 以 QRTZ_TRIGGERS 为准)。</summary>
        public DateTimeOffset? NextRunTime { get; }

        /// <summary>连续失败次数(成功 / 跳过会归零)。</summary>
        public int ConsecutiveFailureCount { get; }

        /// <summary>上次错误描述。</summary>
        public string LastError { get; }

        /// <summary>上次执行的状态码。</summary>
        public HandlerExecutionStatus? LastStatus { get; }
    }
}
