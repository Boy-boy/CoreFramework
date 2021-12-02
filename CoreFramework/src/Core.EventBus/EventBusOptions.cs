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

        /// <summary>
        /// 自动订阅，Handler处理器所属的程序集集合
        /// </summary>
        public Assembly[] HandlersAssemblies { get; set; }

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
