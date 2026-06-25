namespace Core.Scheduling.Models
{
    /// <summary>
    /// 一次执行的结果状态。
    /// </summary>
    public enum HandlerExecutionStatus
    {
        /// <summary>
        /// 执行成功。
        /// </summary>
        Success = 1,

        /// <summary>
        /// 业务执行失败：handler 自己识别问题并返回，不抛异常。
        /// 调度运行时按退避策略安排下一次。
        /// </summary>
        Failure = 2,

        /// <summary>
        /// 主动跳过：例如非交易时段、前置条件未满足。
        /// 不计入失败次数，不影响下次按正常节奏触发。
        /// </summary>
        Skipped = 3,

        /// <summary>
        /// 因取消令牌触发而中断。
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// 框架级异常：handler 抛出异常未被自身处理；
        /// 由运行时统一捕获并记入失败计数。
        /// </summary>
        Faulted = 5
    }
}
