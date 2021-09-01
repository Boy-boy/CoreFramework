using System;
using System.Linq;
using System.Reflection;

namespace Core.EventBus
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageGroupAttribute : Attribute
    {
        public virtual string Group { get; }

        public MessageGroupAttribute(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException($"{nameof(group)} can not be null, empty or white space!");
            }
            Group = group;
        }

        public static string GetGroupOrDefault(Type messageType)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }
            return messageType
                       .GetCustomAttributes(true)
                       .OfType<MessageGroupAttribute>()
                       .FirstOrDefault()
                       ?.Group
                   ?? Assembly.GetEntryAssembly()?.GetName().Name.ToLower();
        }
    }
}
