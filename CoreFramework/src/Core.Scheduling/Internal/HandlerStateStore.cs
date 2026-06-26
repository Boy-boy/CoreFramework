using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Core.Scheduling.Abstractions;
using Core.Scheduling.Models;

namespace Core.Scheduling.Internal
{
    /// <summary>
    /// 进程内 handler 状态存储(单例)。
    /// 同时实现 <see cref="IHandlerExecutionInspector"/> 对外暴露只读视图。
    /// </summary>
    internal sealed class HandlerStateStore : IHandlerExecutionInspector
    {
        private readonly ConcurrentDictionary<string, HandlerStateRecord> _records
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>取(必要时新建)某个 handler 的可变记录。</summary>
        public HandlerStateRecord Get(string handlerCode)
            => _records.GetOrAdd(handlerCode, code => new HandlerStateRecord(code));

        /// <summary>移除指定 handler 的状态(孤儿清理场景使用)。</summary>
        public void Remove(string handlerCode) => _records.TryRemove(handlerCode, out _);

        /// <inheritdoc />
        public HandlerState GetState(string handlerCode)
            => _records.TryGetValue(handlerCode, out var rec) ? rec.Snapshot() : null;

        /// <inheritdoc />
        public IReadOnlyCollection<HandlerState> GetAllStates()
            => _records.Values.Select(r => r.Snapshot()).ToArray();
    }

    /// <summary>
    /// 单个 handler 的可变状态记录,所有读写都加锁保证 <see cref="Snapshot"/> 一致性。
    /// </summary>
    /// <remarks>
    /// 两个聚合 API 压低锁频次:
    /// <see cref="ReadDispatchSnapshot"/> 一把锁拿到 NextRunTime + IsRunning;
    /// <see cref="MarkFinished"/> 把"推进失败计数 + 写回"装进同一把锁,避免读写竞争。
    /// </remarks>
    internal sealed class HandlerStateRecord
    {
        private readonly Lock _gate = new();
        private bool _isRunning;
        private DateTimeOffset? _lastStartTime;
        private DateTimeOffset? _lastFinishTime;
        private DateTimeOffset? _lastSuccessTime;
        private DateTimeOffset? _nextRunTime;
        private int _consecutiveFailureCount;
        private string _lastError;
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

        /// <summary>主循环 dispatch 用的一次性读:同一把锁里取 NextRunTime + IsRunning。</summary>
        public void ReadDispatchSnapshot(out DateTimeOffset? nextRunTime, out bool isRunning)
        {
            lock (_gate)
            {
                nextRunTime = _nextRunTime;
                isRunning = _isRunning;
            }
        }

        /// <summary>仅设置首次 NextRunTime(含 StartDelay),不动 IsRunning / Last*。</summary>
        public void MarkInitialized(DateTimeOffset nextRunTime)
        {
            lock (_gate)
            {
                _nextRunTime ??= nextRunTime;
            }
        }

        /// <summary>标记开始执行,同步写入乐观 <paramref name="tentativeNextRun"/> 防止主循环重复触发。</summary>
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
        /// 标记执行完成:同一把锁里更新最后执行状态 + 推进失败计数。
        /// </summary>
        /// <remarks>
        /// 不算下次触发时间,那是宿主层的活。
        /// BG 由 <c>BackgroundNextRunFilter</c> 之后调用 <see cref="TryUpdateNextRunTime"/> 写入;
        /// Hangfire / Quartz 模式下 <see cref="NextRunTime"/> 保留 <see cref="MarkStarted"/> 写入的 tentative 值,
        /// 外部引擎以自家计算为准。
        /// </remarks>
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
                        break;
                    case HandlerExecutionStatus.Failure:
                    case HandlerExecutionStatus.Faulted:
                        _consecutiveFailureCount = unchecked(_consecutiveFailureCount + 1);
                        break;
                }
            }
        }

        /// <summary>
        /// 基于最后执行状态 + 失败计数,通过 <paramref name="computeNextRun"/> 委托算下次时间并写回(仅 BG 调用)。
        /// </summary>
        /// <remarks>
        /// 全程在同一把锁内:若发现新一轮 <see cref="MarkStarted"/> 已落地(<c>_isRunning=true</c>)则直接放弃,
        /// 不覆盖新执行的 tentativeNext,避免用过期失败计数写脏数据。
        /// 故意用委托而非 <c>INextRunStrategy</c> 接口——后者是 BG 包概念,共享核不依赖。
        /// </remarks>
        /// <param name="computeNextRun">(最后状态, 连续失败数, 最后完成时间) → 下次触发时间;返回 null 表示不更新。</param>
        public void TryUpdateNextRunTime(
            Func<HandlerExecutionStatus, int, DateTimeOffset, DateTimeOffset?> computeNextRun)
        {
            lock (_gate)
            {
                if (_isRunning) return;
                if (_lastStatus is null) return;

                var next = computeNextRun(
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
