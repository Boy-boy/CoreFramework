namespace Core.Scheduling.Models
{
    /// <summary>一次执行的结果状态。</summary>
    public enum HandlerExecutionStatus
    {
        /// <summary>执行成功。</summary>
        Success = 1,

        /// <summary>业务失败:handler 自识别返回(未抛异常),按退避策略排下次。</summary>
        Failure = 2,

        /// <summary>主动跳过:不计失败,下次按正常节奏触发。</summary>
        Skipped = 3,

        /// <summary>因取消令牌触发而中断。</summary>
        Cancelled = 4,

        /// <summary>框架级异常:handler 抛出未处理异常,运行时统一兜底并计入失败计数。</summary>
        Faulted = 5
    }
}
