using System;
using System.Collections.Generic;

namespace Core.EventBus
{
    /// <summary>
    /// <see cref="IMessage"/> 的默认实现;业务事件 / 集成事件继承本类即可。
    /// </summary>
    public class Message : IMessage
    {
        /// <summary>分配新的 Id、UTC 时间戳和空元数据容器。</summary>
        /// <remarks>
        /// Id 使用 UUIDv7(<see cref="Guid.CreateVersion7()"/>):高 48 位是 Unix 毫秒时间戳,低位为随机熵。
        /// 仍是 128 位 Guid,跟 v4 在类型 / 字符串 / DB 列层面完全互通;额外好处是 outbox / inbox 表按
        /// Id 索引时连续插入落在 B-tree 最右叶子页,避免随机 v4 频繁页分裂。
        /// </remarks>
        public Message()
        {
            Id = Guid.CreateVersion7();
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
