using Core.Modularity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.Api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureServiceCollection<StartupModule>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.BuildApplicationBuilder();
        }
    }
}
