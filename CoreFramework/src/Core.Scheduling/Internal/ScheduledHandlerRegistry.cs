using System;
using System.Collections.Generic;
using System.Linq;
using Core.Scheduling.Abstractions;

namespace Core.Scheduling.Internal
{
    /// <summary>
    /// 默认处理器注册表。构造期校验 HandlerCode 非空且全局唯一(重复直接抛,不静默覆盖);
    /// handler 数组在构造期缓存一次,主循环零分配。
    /// </summary>
    internal sealed class ScheduledHandlerRegistry : IScheduledHandlerRegistry
    {
        private readonly Dictionary<string, IScheduledHandler> _byCode;
        private readonly IScheduledHandler[] _handlers;

        public ScheduledHandlerRegistry(IEnumerable<IScheduledHandler> handlers)
        {
            if (handlers == null) throw new ArgumentNullException(nameof(handlers));

            _byCode = new Dictionary<string, IScheduledHandler>(StringComparer.OrdinalIgnoreCase);
            foreach (var handler in handlers)
            {
                if (string.IsNullOrWhiteSpace(handler.HandlerCode))
                {
                    throw new InvalidOperationException(
                        $"Scheduled handler '{handler.GetType().FullName}' has an empty HandlerCode.");
                }

                if (handler.Schedule == null)
                {
                    throw new InvalidOperationException(
                        $"Scheduled handler '{handler.HandlerCode}' has null Schedule descriptor.");
                }

                if (_byCode.TryGetValue(handler.HandlerCode, out var existing))
                {
                    throw new InvalidOperationException(
                        $"Duplicate HandlerCode '{handler.HandlerCode}': "
                        + $"{existing.GetType().FullName} vs {handler.GetType().FullName}. "
                        + "HandlerCode must be globally unique within the scheduling runtime.");
                }

                _byCode.Add(handler.HandlerCode, handler);
            }

            _handlers = _byCode.Values.ToArray();
        }

        public IReadOnlyCollection<IScheduledHandler> GetHandlers() => _handlers;

        public IScheduledHandler Find(string handlerCode)
            => string.IsNullOrEmpty(handlerCode)
                ? null
                : _byCode.GetValueOrDefault(handlerCode);
    }
}
