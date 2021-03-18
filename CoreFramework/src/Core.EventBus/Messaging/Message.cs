using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Core.EventBus
{
    public class Message : IMessage
    {
        public Message()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
            Items = new Dictionary<string, string>();
        }

        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public IDictionary<string, string> Items { get; protected set; }

        public void AddItems(IDictionary<string, string> items)
        {
            if (items == null || Items.Count == 0)
                return;

            if (Items == null)
                Items = new ConcurrentDictionary<string, string>();

            foreach (var entry in items)
            {
                if (!Items.ContainsKey(entry.Key))
                {
                    Items.Add(entry.Key, entry.Value);
                }
            }
        }

        public void RemoveItem(string itemKey)
        {
            if (Items == null)
                return;

            if (Items.ContainsKey(itemKey))
                Items.Remove(itemKey);
        }
    }
}
