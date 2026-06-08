using System.Collections.Concurrent;

namespace Core.Alert
{
    /// <summary>
    /// 按 sessionKey 复用 <see cref="AlertEscalationSession"/> 实例。
    /// 同一个 key 在当前进程内必须共享同一个 Session，Session 内部的信号量串行化才有意义。
    /// </summary>
    public sealed class AlertEscalationManager
    {
        private readonly IAlertStorageProvider _storageProvider;
        private readonly ConcurrentDictionary<string, AlertEscalationSession> _sessions = new();

        public AlertEscalationManager(IAlertStorageProvider storageProvider)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        }

        /// <summary>
        /// 获取指定会话；不存在时创建。
        /// </summary>
        public AlertEscalationSession GetOrCreate(string sessionKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionKey);
            return _sessions.GetOrAdd(sessionKey, key => new AlertEscalationSession(key, _storageProvider));
        }
    }
}
