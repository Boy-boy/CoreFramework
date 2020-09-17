using System;
using System.Linq;

namespace Core.EventBus
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventNameAttribute : Attribute
    {
        public virtual string Name { get; }

        public EventNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"{nameof(name)} can not be null, empty or white space!");
            }
            Name = name;
        }

        public static string GetNameOrDefault(Type eventType)
        {
            if (eventType == null)
            {
                throw new ArgumentNullException(nameof(eventType));
            }
            return eventType
                       .GetCustomAttributes(true)
                       .OfType<EventNameAttribute>()
                       .FirstOrDefault()
                       ?.Name
                   ?? eventType.FullName;
        }
    }
}
