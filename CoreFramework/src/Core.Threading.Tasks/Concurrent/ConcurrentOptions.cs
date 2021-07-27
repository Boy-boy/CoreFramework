using System;

namespace Core.Threading.Tasks
{
    public class ConcurrentOptions
    {
        /// <summary>
        /// 最大并发量,默认为10
        /// </summary>
        public int MaxConcurrentSize { get; set; } = 10;

        /// <summary>
        /// 达到最大并发数，开始挂起，默认挂起20毫秒
        /// </summary>
        public int SleepMillisecond { get; set; } = 20;

        /// <summary>
        /// 任务结果生命周期，默认24小时
        /// </summary>
        public TimeSpan LifeTime { get; } = TimeSpan.FromHours(24);
    }
}
