using System.Diagnostics;
using System.Threading.Tasks;
using Core.EventBus;
using Core.EventBus.Integration;
using Core.EventBus.Local;
using Core.EventBus.Transaction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PublishApi.Event;

namespace PublishApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IIntegrationMessagePublisher _publisher;
        private readonly ILocalMessagePublisher _localPublisher;
        private readonly IConfiguration _configuration;

        public WeatherForecastController(
            IIntegrationMessagePublisher publisher,
            ILocalMessagePublisher localPublisher,
            IConfiguration configuration)
        {
            _publisher = publisher;
            _localPublisher = localPublisher;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var sw = new Stopwatch();
            sw.Start();
            var connection = new SqlConnection(_configuration.GetConnectionString("customer"));
            using var transaction = connection.BeginTransaction(_publisher);
            for (var i = 0; i < 500; i++)
            {
                await _publisher.PublishAsync(new CustomerEvent());
                await _localPublisher.PublishAsync(new CustomerEvent());
            }
            await transaction.CommitAsync();
            sw.Stop();
            return $"500个事件，耗时：{sw.ElapsedMilliseconds}";
        }
    }
}
