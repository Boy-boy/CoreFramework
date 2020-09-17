using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Core.Modularity
{
    public class ServiceCollectionContext
    {
        public IServiceCollection Services { get; }
        public IDictionary<string, object> Items { get; }

        public object this[string key]
        {
            get => Items[key];
            set => Items[key] = value;
        }
        public ServiceCollectionContext(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Items = new Dictionary<string, object>();
        }

    }
}
