using Core.EventBus;
using Core.EventBus.Transaction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PublishApi.Event;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PublishApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly IMessagePublisher _publisher;
        private readonly IConfiguration _configuration;
        private readonly ITransactionAccessor _transactionAccessor;

        public WeatherForecastController(
            IMessagePublisher publisher,
            IConfiguration configuration,
            ITransactionAccessor transactionAccessor)
        {
            _publisher = publisher;
            _configuration = configuration;
            _transactionAccessor = transactionAccessor;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var connection = new SqlConnection(_configuration.GetConnectionString("customer"));
            connection.BeginTransaction(_publisher);
            for (int i = 0; i < 10; i++)
            {
                _publisher.PublishAsync(new CustomerEvent());
            }
            _transactionAccessor.Transaction?.Commit();
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
