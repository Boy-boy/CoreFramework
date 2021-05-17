using System;
using System.Collections.Generic;
using System.Reflection;

namespace Core.EventBus
{
    public class EventBusOptions
    {
        public EventBusOptions()
        {
            Extensions = new List<IEventBusOptionsExtensions>();
        }
        public Assembly[] AutoRegistrarHandlersAssemblies { get; set; }

        public List<IEventBusOptionsExtensions> Extensions { get; set; }
    }

    public static class EventBusOptionsExtensions
    {
        public static void AddExtensions(this EventBusOptions options, IEventBusOptionsExtensions eventBusOptionExtensions)
        {
            if (eventBusOptionExtensions == null)
                throw new AggregateException(nameof(eventBusOptionExtensions));
            options.Extensions.Add(eventBusOptionExtensions);
        }
    }
}
