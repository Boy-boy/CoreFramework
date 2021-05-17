using System;
using System.Linq;

namespace Core.EntityFrameworkCore
{
    public class DbContextNameAttribute:Attribute
    {
        public virtual string Name { get; }

        public DbContextNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"{nameof(name)} can not be null, empty or white space!");
            }
            Name = name;
        }

        public static string GetNameOrDefault(Type messageType)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }
            return messageType
                       .GetCustomAttributes(true)
                       .OfType<DbContextNameAttribute>()
                       .FirstOrDefault()
                       ?.Name
                   ?? messageType.FullName;
        }
    }
}
