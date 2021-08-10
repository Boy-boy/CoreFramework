using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Threading.Tasks
{
    public class ConcurrentResult
    {
        private static readonly TimerCallback TimerCallback = s => ((ConcurrentResult)s)?.Timer_Tick();
        private bool _timerInitialized;
        private TimerCallback _callback;
        private Timer _timer;
        private readonly object _lock;

        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName { get; }

        /// <summary>
        /// 任务结果生命周期
        /// </summary>
        public TimeSpan LifeTime { get; }

        public ConcurrentResult(string taskName,
            int taskCount,
            TimeSpan lifeTime)
        {
            TaskName = taskName;
            TaskCount = taskCount;
            LifeTime = lifeTime;
            _lock = new object();
        }

        internal bool Completed;

        /// <summary>
        /// 总任务数
        /// </summary>
        internal int TaskCount;

        /// <summary>
        /// 正在执行的任务集合
        /// </summary>
        internal List<Task> ExecuteTaskList = new List<Task>();

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted => Completed;

        /// <summary>
        /// 任务完成率
        /// </summary>
        public double FinishingRate =>
            TaskCount == 0
                ? 1
                : Math.Round((double)ExecuteTaskList.Count(p => p.Status == TaskStatus.RanToCompletion) / TaskCount, 4);

        /// <summary>
        /// 开启定时器
        /// </summary>
        /// <param name="callback"></param>
        internal void StartExpiryTimer(TimerCallback callback)
        {
            if (LifeTime == Timeout.InfiniteTimeSpan || Volatile.Read(ref _timerInitialized))
                return;
            StartExpiryTimerSlow(callback);
        }

        /// <summary>
        /// 结束定时器
        /// </summary>
        internal void StopExpiryTimer()
        {
            lock (_lock)
            {
                if (_timer == null)
                    return;
                _timer.Dispose();
                _timer = null;
                _callback = null;
            }
        }

        private void StartExpiryTimerSlow(TimerCallback callback)
        {
            lock (_lock)
            {
                if (Volatile.Read(ref _timerInitialized))
                    return;
                _callback = callback;
                _timer = new Timer(TimerCallback, this, LifeTime, Timeout.InfiniteTimeSpan);
                _timerInitialized = true;
            }
        }

        private void Timer_Tick()
        {
            lock (_lock)
            {
                if (_timer == null)
                    return;
                _timer.Dispose();
                _timer = null;
                _callback(this);
            }
        }
    }
}
