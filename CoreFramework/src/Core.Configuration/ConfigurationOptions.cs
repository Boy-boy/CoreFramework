using System;
using System.Collections.Generic;

namespace Core.Configuration
{
    public class ConfigurationOptions
    {
        public ConfigurationOptions()
        {
            Extensions = new List<IConfigurationOptionsExtensions>();
        }
        public List<IConfigurationOptionsExtensions> Extensions { get; set; }
    }

    public static class EventBusOptionsExtensions
    {
        public static void AddExtensions(this ConfigurationOptions options, IConfigurationOptionsExtensions eventBusOptionExtensions)
        {
            if (eventBusOptionExtensions == null)
                throw new AggregateException(nameof(eventBusOptionExtensions));
            options.Extensions.Add(eventBusOptionExtensions);
        }
    }
}
