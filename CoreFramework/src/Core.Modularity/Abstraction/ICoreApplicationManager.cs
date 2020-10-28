using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Modularity.Abstraction
{
    public interface ICoreApplicationManager
    {
        void ConfigureConfigureServices(IServiceCollection services);
        void Configure(IApplicationBuilder app);
        void Shutdown(IServiceProvider serviceProvider);

    }
}
