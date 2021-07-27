using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Threading.Tasks
{
    /// <summary>
    /// 并发扩展库
    /// </summary>
    public class ConcurrentManagement
    {
        private static readonly ConcurrentDictionary<string, ConcurrentResult> ConcurrentResults
            = new ConcurrentDictionary<string, ConcurrentResult>();

        /// <summary>
        /// 并发处理任务(阻塞请求线程)
        /// </summary>
        /// <param name="tasks">并发任务集合</param>
        /// <param name="concurrentOptions">并发项</param>
        /// <returns></returns>
        public static void Processing(List<Func<Task>> tasks,
            ConcurrentOptions concurrentOptions = default)
        {
            tasks ??= new List<Func<Task>>();
            if (tasks.Count <= 0)
                return;

            concurrentOptions ??= new ConcurrentOptions();
            var concurrentCount = 0;

            var result = new List<Task>();

            for (var i = 0; i < tasks.Count; i++)
            {
                Task task = null;
                if (Interlocked.Increment(ref concurrentCount) <= concurrentOptions.MaxConcurrentSize)
                {
                    task = tasks[i]()
                        .ContinueWith(t =>
                        {
                            Interlocked.Decrement(ref concurrentCount);
                        });
                }
                else
                {
                    //当达到最大并发量时，值将减一
                    Interlocked.Decrement(ref concurrentCount);
                    i--;
                }
                while (concurrentCount == concurrentOptions.MaxConcurrentSize)
                {
                    Thread.Sleep(concurrentOptions.SleepMillisecond);
                }

                if (task != null)
                    result.Add(task);
            }
            Task.WaitAll(result.ToArray());
        }

        /// <summary>
        /// 并发处理任务（不阻塞请求线程）
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <param name="tasks">并发任务集合</param>
        /// <param name="concurrentOptions">并发项</param>
        /// <returns></returns>
        public static async Task ProcessingAsync(string taskName, List<Func<Task>> tasks,
            ConcurrentOptions concurrentOptions = default)
        {
            tasks ??= new List<Func<Task>>();
            if (tasks.Count <= 0) return;

            await Task.Yield();

            concurrentOptions ??= new ConcurrentOptions();
            var concurrentCount = 0;

            var concurrentResultNew = new ConcurrentResult(taskName, tasks.Count, concurrentOptions.LifeTime);
            TryRemoveValue(taskName, out _);
            TryAddValue(taskName, concurrentResultNew);

            var taskList = new List<Task>();
            for (var i = 0; i < tasks.Count; i++)
            {
                Task task = null;
                if (Interlocked.Increment(ref concurrentCount) <= concurrentOptions.MaxConcurrentSize)
                {
                    task = tasks[i]()
                        .ContinueWith(t =>
                        {
                            Interlocked.Decrement(ref concurrentCount);
                        });
                }
                else
                {
                    //当达到最大并发量时，值将减一
                    Interlocked.Decrement(ref concurrentCount);
                    i--;
                }

                while (concurrentCount == concurrentOptions.MaxConcurrentSize)
                {
                    Thread.Sleep(concurrentOptions.SleepMillisecond);
                }

                if (task != null)
                    taskList.Add(task);

                concurrentResultNew.ExecuteTaskList = taskList;
            }
            await Task.WhenAll(taskList);
            concurrentResultNew.Completed = true;
            concurrentResultNew.StartExpiryTimer(ExpiryTimer_Tick);
        }

        /// <summary>
        /// 获取任务结果值
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetValue(string taskName, out ConcurrentResult result)
        {
            return ConcurrentResults.TryGetValue(taskName, out result);
        }

        /// <summary>
        /// 移除任务结果值
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryRemoveValue(string taskName, out ConcurrentResult result)
        {
            if (!ConcurrentResults.TryRemove(taskName, out result)) return false;
            result.StopExpiryTimer();
            return true;
        }

        /// <summary>
        /// 添加任务结果值
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static bool TryAddValue(string taskName, ConcurrentResult result)
        {
            return ConcurrentResults.TryAdd(taskName, result);
        }

        /// <summary>
        /// 移除任务值回调任务
        /// </summary>
        /// <param name="state"></param>
        private static void ExpiryTimer_Tick(object state)
        {
            var other = (ConcurrentResult)state;
            ConcurrentResults.TryRemove(other.TaskName, out _);
        }
    }
}
