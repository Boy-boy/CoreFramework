using System;
using System.Collections.Generic;

namespace Core.Permission
{
    public class PermissionOptions
    {
        public PermissionOptions()
        {
            Extensions = new List<IPermissionOptionsExtensions>();
        }
        public List<IPermissionOptionsExtensions> Extensions { get; set; }
    }

    public static class EventBusOptionsExtensions
    {
        public static void AddExtensions(this PermissionOptions options, IPermissionOptionsExtensions eventBusOptionExtensions)
        {
            if (eventBusOptionExtensions == null)
                throw new AggregateException(nameof(eventBusOptionExtensions));
            options.Extensions.Add(eventBusOptionExtensions);
        }
    }
}
