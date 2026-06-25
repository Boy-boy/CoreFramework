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
        /// 标记执行完成。共享逻辑:同一把锁里更新最后执行状态 + 推进失败计数。
        /// <para>
        /// 不再算下次触发时间——那是宿主层(BG)的职责。BG 模式由 <c>BackgroundNextRunFilter</c>
        /// 在本方法之后调 <see cref="TryUpdateNextRunTime"/> 写入;
        /// Hangfire/Quartz 模式下不调用 <see cref="TryUpdateNextRunTime"/>,
        /// <see cref="NextRunTime"/> 保留 <see cref="MarkStarted"/> 写入的 tentative 值,
        /// 外部引擎以自家计算为准,本框架的快照仅供 inspector 参考。
        /// </para>
        /// </summary>
        public void MarkFinished(
            DateTimeOffset finishTime,
            HandlerExecutionResult result)
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
            }
        }

        /// <summary>
        /// 基于已落定的最后执行状态 + 失败计数,通过 <paramref name="strategy"/> 算下次时间并写回。
        /// 仅 BG 宿主调用(<c>BackgroundNextRunFilter</c>)。
        /// <para>
        /// 全程在同一把锁里:即使新一轮 <see cref="MarkStarted"/> 已落,本方法见到 <c>_isRunning=true</c>
        /// 会直接放弃 —— 不覆盖新执行的 tentativeNext,避免用过期的失败计数写脏数据。
        /// </para>
        /// </summary>
        public void TryUpdateNextRunTime(ScheduleDescriptor schedule, INextRunStrategy strategy)
        {
            lock (_gate)
            {
                if (_isRunning) return;                 // 新一轮已开始,放弃本次计算结果
                if (_lastStatus is null) return;        // 还没有任何执行完成过

                var next = strategy.ComputeNextRun(
                    schedule,
                    _lastStatus.Value,
                    _consecutiveFailureCount,
                    _lastFinishTime ?? DateTimeOffset.UtcNow);
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
