using Microsoft.AspNetCore.Builder;
using System;

namespace Core.Modularity
{
    public class ApplicationBuilderContext
    {
        public IApplicationBuilder ApplicationBuilder { get; }

        public ApplicationBuilderContext(IApplicationBuilder applicationBuilder)
        {
            ApplicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
        }
    }
}
