using System.Diagnostics;
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
            var sw = new Stopwatch();
            sw.Start();
            var connection = new NpgsqlConnection(_configuration.GetConnectionString("customer"));
            using var transaction = connection.BeginTransaction(_publisher);
            for (var i = 0; i < 500; i++)
            {
                await _publisher.PublishAsync(new CustomerEvent());
            }
            await transaction.CommitAsync();
            sw.Stop();
            return $"500个事件，耗时：{sw.ElapsedMilliseconds}";
        }
    }
}
