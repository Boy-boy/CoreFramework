using Core.Modularity;

namespace ThirdPartySso.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.ConfigureServiceCollection<StartupModule>();
            
            var app = builder.Build();

            app.BuildApplicationBuilder();
            app.Run();
        }
    }
}