using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Models;

namespace Core.Scheduling.Internal
{
    /// <summary>
    /// 进程内 handler 状态存储。单例。
    /// 同时实现 <see cref="IHandlerExecutionInspector"/> 暴露只读视图。
    /// </summary>
    internal sealed class HandlerStateStore : IHandlerExecutionInspector
    {
        private readonly ConcurrentDictionary<string, HandlerStateRecord> _records
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>取（必要时新建）某个 handler 的可变记录。</summary>
        public HandlerStateRecord Get(string handlerCode)
            => _records.GetOrAdd(handlerCode, code => new HandlerStateRecord(code));

        /// <summary>移除一个 handler 的状态（孤儿清理等场景使用）。</summary>
        public void Remove(string handlerCode) => _records.TryRemove(handlerCode, out _);

        /// <inheritdoc />
        public HandlerState? GetState(string handlerCode)
            => _records.TryGetValue(handlerCode, out var rec) ? rec.Snapshot() : null;

        /// <inheritdoc />
        public IReadOnlyCollection<HandlerState> GetAllStates()
            => _records.Values.Select(r => r.Snapshot()).ToArray();
    }

    /// <summary>
    /// 单个 handler 的可变状态记录。所有读写都加锁,确保 <see cref="Snapshot"/> 一致性。
    /// <para>
    /// 两个聚合 API 用来压低锁的频次:
    /// <see cref="ReadDispatchSnapshot"/> 给主循环一次锁拿到 NextRunTime + IsRunning;
    /// <see cref="MarkFinished"/> 把"推进失败计数 + 算下一次时间 + 写回"装进同一把锁,避免读写竞争。
    /// </para>
    /// </summary>
    internal sealed class HandlerStateRecord
    {
        private readonly Lock _gate = new();
        private bool _isRunning;
        private DateTimeOffset? _lastStartTime;
        private DateTimeOffset? _lastFinishTime;
        private DateTimeOffset? _lastSuccessTime;
        private DateTimeOffset? _nextRunTime;
        private int _consecutiveFailureCount;
        private string? _lastError;
        private HandlerExecutionStatus? _lastStatus;

        public HandlerStateRecord(string handlerCode)
        {
            HandlerCode = handlerCode;
        }

        public string HandlerCode { get; }

        public bool IsRunning
        {
            get { lock (_gate) return _isRunning; }
        }

        public DateTimeOffset? NextRunTime
        {
            get { lock (_gate) return _nextRunTime; }
        }

        public int ConsecutiveFailureCount
        {
            get { lock (_gate) return _consecutiveFailureCount; }
        }

        /// <summary>
        /// 主循环 dispatch 用的一次性读:同一把锁里取 NextRunTime + IsRunning,
        /// 避免对同一 record 连续两次加锁。
        /// </summary>
        public void ReadDispatchSnapshot(out DateTimeOffset? nextRunTime, out bool isRunning)
        {
            lock (_gate)
            {
                nextRunTime = _nextRunTime;
                isRunning = _isRunning;
            }
        }

        /// <summary>仅设置首次 NextRunTime（含 StartDelay），不动 IsRunning / Last*。</summary>
        public void MarkInitialized(DateTimeOffset nextRunTime)
        {
            lock (_gate)
            {
                _nextRunTime ??= nextRunTime;
            }
        }

        /// <summary>
        /// 标记开始执行。同步更新乐观 <paramref name="tentativeNextRun"/>，
        /// 防止主循环在 handler 完成前再次触发。
        /// </summary>
        public void MarkStarted(DateTimeOffset startTime, DateTimeOffset tentativeNextRun)
        {
            lock (_gate)
            {
                _isRunning = true;
                _lastStartTime = startTime;
                _nextRunTime = tentativeNextRun;
            }
        }

        /// <summary>
        /// 标记执行完成。同一把锁里推进失败计数,然后通过 <paramref name="strategy"/> 算下次时间,
        /// 避免"读 ConsecutiveFailureCount → 算 next → 写回"分离时的竞争。
        /// strategy 返回 <see langword="null"/>(Hangfire/Quartz 模式)时,NextRunTime 不动,
        /// 由外部引擎决定下次,本框架不汇报。
        /// </summary>
        public void MarkFinished(
            DateTimeOffset finishTime,
            HandlerExecutionResult result,
            ScheduleDescriptor schedule,
            INextRunStrategy strategy)
        {
            lock (_gate)
            {
                _isRunning = false;
                _lastFinishTime = finishTime;
                _lastStatus = result.Status;
                _lastError = result.ErrorMessage;

                switch (result.Status)
                {
                    case HandlerExecutionStatus.Success:
                        _lastSuccessTime = finishTime;
                        _consecutiveFailureCount = 0;
                        break;
                    case HandlerExecutionStatus.Skipped:
                    case HandlerExecutionStatus.Cancelled:
                        // 不动失败计数，也不更新成功时间
                        break;
                    case HandlerExecutionStatus.Failure:
                    case HandlerExecutionStatus.Faulted:
                        _consecutiveFailureCount = unchecked(_consecutiveFailureCount + 1);
                        break;
                }

                var next = strategy.ComputeNextRun(schedule, result.Status, _consecutiveFailureCount, finishTime);
                if (next is not null) _nextRunTime = next;
            }
        }

        /// <summary>生成不可变快照。</summary>
        public HandlerState Snapshot()
        {
            lock (_gate)
            {
                return new HandlerState(
                    HandlerCode,
                    _isRunning,
                    _lastStartTime,
                    _lastFinishTime,
                    _lastSuccessTime,
                    _nextRunTime,
                    _consecutiveFailureCount,
                    _lastError,
                    _lastStatus);
            }
        }
    }
}
