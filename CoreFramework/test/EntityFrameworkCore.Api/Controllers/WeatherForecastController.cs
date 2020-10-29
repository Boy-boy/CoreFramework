using Core.Uow;
using EntityFrameworkCore.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using EntityFrameworkCore.Api.Events;
using System.Threading.Tasks;
using Core.EntityFrameworkCore;

namespace EntityFrameworkCore.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly CoreDbContext _customerDbContext;
        private readonly IUnitOfWork _unitOfWork;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            CoreDbContext customerDbContext,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _customerDbContext = customerDbContext;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("add")]
        public async Task<string> Add()
        {
            var student = new Student("张三", 23);
            student.AddEvent(new AddStudentEvent { AggregateRootId = student.Id });
            _customerDbContext.Add(student);
            await _customerDbContext.SaveChangesAsync();
            return "ok";
        }

    }
}
