using System;
using System.Collections.Generic;

namespace Core.Scheduling.Models
{
    /// <summary>一次触发的执行结果(由 handler 返回,或异常/取消时由运行时兜底)。</summary>
    public sealed class HandlerExecutionResult
    {
        private static readonly IReadOnlyDictionary<string, long> EmptyMetrics
            = new Dictionary<string, long>();

        private HandlerExecutionResult(
            string handlerCode,
            HandlerExecutionStatus status,
            DateTimeOffset startTime,
            DateTimeOffset finishTime,
            string errorMessage,
            Exception exception,
            IReadOnlyDictionary<string, long> metrics)
        {
            HandlerCode = handlerCode;
            Status = status;
            StartTime = startTime;
            FinishTime = finishTime;
            ErrorMessage = errorMessage;
            Exception = exception;
            Metrics = metrics ?? EmptyMetrics;
        }

        /// <summary>处理器编码。</summary>
        public string HandlerCode { get; }

        /// <summary>执行状态。</summary>
        public HandlerExecutionStatus Status { get; }

        /// <summary>开始时间。</summary>
        public DateTimeOffset StartTime { get; }

        /// <summary>完成时间。</summary>
        public DateTimeOffset FinishTime { get; }

        /// <summary>错误描述(业务级或框架级,按 <see cref="Status"/> 区分)。</summary>
        public string ErrorMessage { get; }

        /// <summary>原始异常(仅 <see cref="HandlerExecutionStatus.Faulted"/> 时有值)。</summary>
        public Exception Exception { get; }

        /// <summary>业务自定义指标,例如本次处理条数、命中数。</summary>
        public IReadOnlyDictionary<string, long> Metrics { get; }

        /// <summary>是否成功(仅 <see cref="HandlerExecutionStatus.Success"/>)。</summary>
        public bool IsSuccess => Status == HandlerExecutionStatus.Success;

        /// <summary>执行耗时。</summary>
        public TimeSpan Duration => FinishTime - StartTime;

        /// <summary>构造成功结果。</summary>
        public static HandlerExecutionResult Success(
            string handlerCode,
            DateTimeOffset startTime,
            DateTimeOffset finishTime,
            IReadOnlyDictionary<string, long> metrics = null)
            => new(handlerCode, HandlerExecutionStatus.Success, startTime, finishTime, null, null, metrics);

        /// <summary>构造业务失败结果(不抛异常)。</summary>
        public static HandlerExecutionResult Failure(
            string handlerCode,
            DateTimeOffset startTime,
            DateTimeOffset finishTime,
            string errorMessage,
            IReadOnlyDictionary<string, long> metrics = null)
            => new(handlerCode, HandlerExecutionStatus.Failure, startTime, finishTime, errorMessage, null, metrics);

        /// <summary>构造跳过结果。</summary>
        public static HandlerExecutionResult Skipped(
            string handlerCode,
            DateTimeOffset startTime,
            DateTimeOffset finishTime,
            string reason = null)
            => new(handlerCode, HandlerExecutionStatus.Skipped, startTime, finishTime, reason, null, null);

        /// <summary>构造取消结果(运行时使用)。</summary>
        public static HandlerExecutionResult Cancelled(
            string handlerCode,
            DateTimeOffset startTime,
            DateTimeOffset finishTime)
            => new(handlerCode, HandlerExecutionStatus.Cancelled, startTime, finishTime, "Cancelled by host.", null, null);

        /// <summary>构造框架级异常结果(运行时使用)。</summary>
        public static HandlerExecutionResult Faulted(
            string handlerCode,
            DateTimeOffset startTime,
            DateTimeOffset finishTime,
            Exception exception)
            => new(handlerCode, HandlerExecutionStatus.Faulted, startTime, finishTime, exception?.Message, exception, null);
    }
}
