using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Core.Alert
{
    /// <summary>
    /// 告警升级状态机。
    /// 负责对同一 session 执行“读状态 - 算决策 - 写状态”的异步串行流程。
    /// </summary>
    public sealed class AlertEscalationSession
    {
        private readonly string _sessionKey;
        private readonly IAlertStorageProvider _storage;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public AlertEscalationSession(string sessionKey, IAlertStorageProvider storage)
        {
            _sessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        /// <summary>
        /// 异常发生时调用。
        /// 根据当前业务日、当前时间和升级节奏，判断本轮是否应触发告警。
        /// </summary>
        public async Task<AlertEscalationDecision> OnAnomalyAsync(DateTime businessDay, DateTime now, TimeSpan[] escalationIntervals)
        {
            // 入口统一做去重和排序，保证内部决策始终面对合法、升序的节奏表。
            var schedule = (escalationIntervals ?? Array.Empty<TimeSpan>())
                .Where(interval => interval >= TimeSpan.Zero)
                .Distinct()
                .OrderBy(interval => interval)
                .ToArray();

            if (schedule.Length == 0)
            {
                return AlertEscalationDecision.Silent;
            }

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var current = await _storage.GetAsync(_sessionKey).ConfigureAwait(false);
                var (decision, next) = Evaluate(current, businessDay, now, schedule);

                if (!ReferenceEquals(next, current))
                {
                    await _storage.SetAsync(_sessionKey, next).ConfigureAwait(false);
                }

                return decision;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// 异常恢复时调用。
        /// 若当前业务日存在未清理的异常状态，则清理状态并返回 true，供调用方发送恢复通知。
        /// </summary>
        public async Task<bool> OnRecoveredAsync(DateTime businessDay)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var current = await _storage.GetAsync(_sessionKey).ConfigureAwait(false);
                if (current == null || current.BusinessDay != businessDay.Date)
                {
                    return false;
                }

                await _storage.RemoveAsync(_sessionKey).ConfigureAwait(false);
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// 状态机核心决策函数。
        /// 任何状态变更都通过创建新快照完成，避免原地修改导致的共享引用问题。
        /// </summary>
        private static (AlertEscalationDecision Decision, AlertSessionState NewState) Evaluate(
            AlertSessionState current,
            DateTime businessDay,
            DateTime now,
            TimeSpan[] escalationIntervals)
        {
            // 跨日或首次异常：以当前时刻为基准，固化整个业务日的升级触发时刻。
            if (current == null || current.BusinessDay != businessDay.Date)
            {
                current = new AlertSessionState
                {
                    BusinessDay = businessDay.Date,
                    TotalSlots = escalationIntervals.Length,
                    Pending = escalationIntervals.Select((interval, index) => new PendingAlertState
                    {
                        Sequence = index + 1,
                        FireAt = now.Add(interval),
                    }).ToList(),
                };
            }

            // 已经发完全部升级档位，后续静默直到恢复或跨日。
            if (current.Pending.Count == 0)
            {
                return (AlertEscalationDecision.Silent, current);
            }

            var nextSlot = current.Pending[0];
            // 下一档尚未到时刻，本轮静默。
            if (now < nextSlot.FireAt)
            {
                return (AlertEscalationDecision.Silent, current);
            }

            // 触发队首档位，并返回去掉该档位后的新快照。
            var newState = new AlertSessionState
            {
                BusinessDay = current.BusinessDay,
                TotalSlots = current.TotalSlots,
                Pending = current.Pending.Skip(1).ToList(),
            };

            return (AlertEscalationDecision.Fire(nextSlot.Sequence, current.TotalSlots), newState);
        }
    }
}
