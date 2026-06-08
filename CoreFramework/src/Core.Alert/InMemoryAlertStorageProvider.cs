using System.Collections.Concurrent;

namespace Core.Alert
{
    /// <summary>
    /// 进程内内存存储实现。
    /// 适合单实例、无重启状态保留要求的场景；框架默认使用该实现以保证开箱即用。
    /// </summary>
    public sealed class InMemoryAlertStorageProvider : IAlertStorageProvider
    {
        private readonly ConcurrentDictionary<string, AlertSessionState> _store = new();

        public Task<AlertSessionState> GetAsync(string sessionKey)
        {
            _store.TryGetValue(sessionKey, out var state);
            return Task.FromResult(state);
        }

        public Task SetAsync(string sessionKey, AlertSessionState state)
        {
            _store[sessionKey] = state;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string sessionKey)
        {
            _store.TryRemove(sessionKey, out _);
            return Task.CompletedTask;
        }
    }
}
