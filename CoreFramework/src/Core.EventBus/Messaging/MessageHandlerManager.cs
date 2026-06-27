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
        /// <remarks>幂等:同 (messageType, handlerType) 重复注册直接返回不抛错,适配启动期被多次扫描程序集的场景。
        /// 但 messageName 撞车(不同 messageType 对应同 MessageNameAttribute 值)仍会抛错,这是真正的配置冲突。</remarks>
        public void AddHandler(Type messageType, Type handlerType)
        {
            lock (_writeLock)
            {
                var current = _wrappers;
                if (current.Any(w => w.MessageType == messageType && w.HandlerType == handlerType))
                {
                    // 幂等返回:重复扫描程序集 / 多模块并存时同一对会被注册多次
                    return;
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
