using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Threading.Tasks
{
    /// <summary>
    /// 并发扩展库
    /// </summary>
    public class ConcurrentManager
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
            tasks = tasks.Where(taskFunc => taskFunc != null).ToList();
            if (tasks.Count <= 0)
                return;

            concurrentOptions ??= new ConcurrentOptions();

            var semaphoreSlim = new SemaphoreSlim(concurrentOptions.MaxConcurrentCount);
            var taskList = new List<Task>();
            foreach (var taskItem in tasks)
            {
                semaphoreSlim.Wait(concurrentOptions.SecondsTimeout * 1000);

                var task = taskItem()
                    .ContinueWith(t =>
                    {
                        semaphoreSlim.Release();
                    });

                taskList.Add(task);
            }
            Task.WaitAll(taskList.ToArray());
        }

        /// <summary>
        /// 并发处理任务（不阻塞请求线程）
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <param name="tasks">并发任务集合</param>
        /// <param name="concurrentOptions">并发项</param>
        /// <returns></returns>
        public static async Task ProcessingAsync(string taskName,
            List<Func<Task>> tasks,
            ConcurrentOptions concurrentOptions = default)
        {
            if (string.IsNullOrEmpty(taskName))
                throw new ArgumentNullException(nameof(taskName));

            tasks ??= new List<Func<Task>>();
            tasks = tasks.Where(taskFunc => taskFunc != null).ToList();
            if (tasks.Count <= 0) return;

            await Task.Yield();

            concurrentOptions ??= new ConcurrentOptions();

            var concurrentResultNew = new ConcurrentResult(taskName, tasks.Count, concurrentOptions.LifeTime);
            TryRemoveValue(taskName, out _);
            TryAddValue(taskName, concurrentResultNew);

            var semaphoreSlim = new SemaphoreSlim(concurrentOptions.MaxConcurrentCount);
            var taskList = new List<Task>();
            foreach (var taskItem in tasks)
            {
                await semaphoreSlim.WaitAsync(concurrentOptions.SecondsTimeout * 1000);

                var task = taskItem()
                    .ContinueWith(t =>
                    {
                        semaphoreSlim.Release();
                    });

                taskList.Add(task);
                concurrentResultNew.AddExecuteTask(task);
            }
            await Task.WhenAll(taskList);
            concurrentResultNew.ExecuteCompleted();
            concurrentResultNew.StartExpiryTimer(ExpiryTimer_Tick);
        }

        /// <summary>
        /// 尝试获取任务结果值
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetValue(string taskName, out IConcurrentResult result)
        {
            result = null;
            if (!ConcurrentResults.TryGetValue(taskName, out var result1))
                return false;
            result = result1;
            return true;
        }

        /// <summary>
        /// 尝试移除任务结果值
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryRemoveValue(string taskName, out IConcurrentResult result)
        {
            result = null;
            if (!ConcurrentResults.TryRemove(taskName, out var result1))
                return false;
            result1.StopExpiryTimer();
            result = result1;
            return true;
        }

        /// <summary>
        /// 添加任务结果值
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static bool TryAddValue(string taskName, ConcurrentResult result)
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
