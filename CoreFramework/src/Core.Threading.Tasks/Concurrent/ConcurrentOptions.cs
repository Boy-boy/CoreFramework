using System;

namespace Core.Threading.Tasks
{
    public class ConcurrentOptions
    {
        /// <summary>
        /// 最小并发数
        /// </summary>
        private readonly int _minimumConcurrentCount = 1;

        /// <summary>
        /// 默认并发数
        /// </summary>
        private int _defaultConcurrentCount = 10;

        /// <summary>
        /// 最大并发量,默认为10
        /// </summary>
        public int MaxConcurrentCount
        {
            get => _defaultConcurrentCount;
            set
            {
                if (value < _minimumConcurrentCount)
                    throw new ArgumentException(nameof(value));
                _defaultConcurrentCount = value;
            }
        }

        /// <summary>
        /// 达到最大并发数,若在这段时间内没有接收到信号则跳过等待继续执行
        /// </summary>
        public int SecondsTimeout { get; set; } = 60;

        /// <summary>
        /// 任务结果生命周期，默认24小时
        /// </summary>
        public TimeSpan LifeTime { get; } = TimeSpan.FromHours(24);
    }
}
