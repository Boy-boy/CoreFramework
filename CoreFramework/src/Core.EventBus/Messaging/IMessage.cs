using System;
using System.Collections.Generic;

namespace Core.EventBus
{
    public interface IMessage
    {
        Guid Id { get; set; }

        DateTime Timestamp { get; set; }

        IDictionary<string, string> Items { get; }

        void AddItems(IDictionary<string, string> items);

        void RemoveItem(string itemKey);
    }
}
