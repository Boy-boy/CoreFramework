using System;
using System.Collections.Generic;
using System.Reflection;

namespace Core.EventBus
{
    public class EventBusOptions
    {
        private Assembly[] _messageHandlerAssemblies;

        public EventBusOptions()
        {
            Extensions = new List<IEventBusOptionsExtensions>();
        }

        public Assembly[] MessageHandlerAssemblies => _messageHandlerAssemblies;

        public List<IEventBusOptionsExtensions> Extensions { get; set; }

        /// <summary>
        /// 注册消费者
        /// </summary>
        /// <param name="assemblies"></param>
        public void AddConsumers(params Assembly[] assemblies)
        {
            _messageHandlerAssemblies = assemblies;
        }
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
