using System.Threading.Tasks;
using Core.EventBus;
using Core.EventBus.Transaction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PublishApi.Event;

namespace PublishApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IMessagePublisher _publisher;
        private readonly IConfiguration _configuration;

        public WeatherForecastController(
            IMessagePublisher publisher,
            IConfiguration configuration)
        {
            _publisher = publisher;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var connection = new NpgsqlConnection(_configuration.GetConnectionString("customer"));
            using var transaction = connection.BeginTransaction(_publisher);
            for (var i = 0; i < 100; i++)
            {
                await _publisher.PublishAsync(new CustomerEvent());
            }
            if (transaction != null)
                await transaction.CommitAsync();

            return "Hello Word";
        }
    }
}
