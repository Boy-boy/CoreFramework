using System;

namespace Core.Modularity
{
    public class ShutdownApplicationContext
    {
        public IServiceProvider ServiceProvider { get; }

        public ShutdownApplicationContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
    }
}
