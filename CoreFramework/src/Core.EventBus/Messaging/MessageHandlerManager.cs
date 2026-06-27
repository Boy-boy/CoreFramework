using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.EventBus
{
    /// <summary>
    /// 线程安全的订阅项注册表:读走不可变快照,写走 copy-on-write,publisher 遍历时不会被并发订阅打断。
    /// </summary>
    public class MessageHandlerManager : IMessageHandlerManager
    {
        private readonly Lock _writeLock = new();

        // 写在 lock 内做 copy-on-write,读直接取这个引用 —— 无锁、零拷贝枚举
        private volatile IReadOnlyList<IMessageHandlerWrapper> _wrappers = Array.Empty<IMessageHandlerWrapper>();

        /// <inheritdoc />
        public event EventHandler<Type> OnEventRemoved;

        /// <inheritdoc />
        public IReadOnlyList<IMessageHandlerWrapper> MessageHandlerWrappers => _wrappers;

        /// <inheritdoc />
        public void AddHandler(Type messageType, Type handlerType)
        {
            lock (_writeLock)
            {
                var current = _wrappers;
                if (current.Any(w => w.MessageType == messageType && w.HandlerType == handlerType))
                {
                    throw new ArgumentException(
                        $"Handler Type {handlerType.Name} already registered for '{messageType.Name}'");
                }

                var messageName = MessageNameAttribute.GetNameOrDefault(messageType);
                if (current.Any(w => w.MessageName == messageName && w.MessageType != messageType))
                {
                    throw new ArgumentException(
                        $"The message name '{messageName}' corresponding to the message type '{messageType}' already exists");
                }

                var wrapperType = typeof(MessageHandlerWrapper<>).MakeGenericType(messageType);
                var wrapper = (IMessageHandlerWrapper)Activator.CreateInstance(wrapperType, handlerType)!;

                var next = new List<IMessageHandlerWrapper>(current.Count + 1);
                next.AddRange(current);
                next.Add(wrapper);
                _wrappers = next;
            }
        }

        /// <inheritdoc />
        public void RemoveHandler(Type messageType, Type handlerType)
        {
            bool messageTypeFullyRemoved;
            lock (_writeLock)
            {
                var current = _wrappers;
                var idx = -1;
                for (var i = 0; i < current.Count; i++)
                {
                    if (current[i].MessageType != messageType || current[i].HandlerType != handlerType) continue;
                    idx = i;
                    break;
                }

                if (idx < 0)
                {
                    return;
                }

                var next = new List<IMessageHandlerWrapper>(current.Count - 1);
                for (var i = 0; i < current.Count; i++)
                {
                    if (i != idx) next.Add(current[i]);
                }
                _wrappers = next;

                messageTypeFullyRemoved = next.All(w => w.MessageType != messageType);
            }

            // 回调放 lock 外,避免订阅者重入引发死锁
            if (messageTypeFullyRemoved)
            {
                OnEventRemoved?.Invoke(this, messageType);
            }
        }
    }
}
