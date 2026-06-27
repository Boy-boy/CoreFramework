using System;
using System.Collections.Generic;

namespace Core.EventBus
{
    /// <summary>
    /// <see cref="IMessage"/> 的默认实现;业务事件 / 集成事件继承本类即可。
    /// </summary>
    public class Message : IMessage
    {
        /// <summary>分配新的 Guid、UTC 时间戳和空元数据容器。</summary>
        public Message()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            Items = new Dictionary<string, string>();
        }

        /// <inheritdoc />
        public Guid Id { get; set; }

        /// <inheritdoc />
        public DateTime Timestamp { get; set; }

        /// <inheritdoc />
        public IDictionary<string, string> Items { get; protected set; }

        /// <inheritdoc />
        public void AddItems(IDictionary<string, string> items)
        {
            if (items == null || items.Count == 0)
                return;

            // 不覆盖已有 key:业务可在发布前预置元数据
            foreach (var entry in items)
            {
                if (!Items.ContainsKey(entry.Key))
                {
                    Items.Add(entry.Key, entry.Value);
                }
            }
        }

        /// <inheritdoc />
        public void RemoveItem(string itemKey)
        {
            if (Items == null)
                return;

            if (Items.ContainsKey(itemKey))
                Items.Remove(itemKey);
        }
    }
}
