using Microsoft.AspNetCore.Builder;
using System;

namespace Core.Modularity
{
    public class ConfigureApplicationContext
    {
        public IApplicationBuilder ApplicationBuilder { get; }

        public ConfigureApplicationContext(IApplicationBuilder applicationBuilder)
        {
            ApplicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
        }
    }
}
