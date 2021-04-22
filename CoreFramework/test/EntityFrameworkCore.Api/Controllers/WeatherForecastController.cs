using Core.Ddd.Domain.Repositories;
using Core.EntityFrameworkCore.Repositories;
using Core.Uow;
using EntityFrameworkCore.Api.Entities;
using EntityFrameworkCore.Api.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IRepository<Student> _repository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
           IRepository<Student> repository,
           IUnitOfWorkManager unitOfWorkManager)
        {
            _logger = logger;
            _repository = repository;
            _unitOfWorkManager = unitOfWorkManager;
        }

        [HttpGet]
        public IEnumerable<Student> Get()
        {
            return _repository.FindAll(s => true);
        }

        [HttpGet("add")]
        public async Task<Student> Add()
        {
            var student = new Student("张三", 24);
            student.AddEvent(new AddStudentEvent { AggregateRootId = student.Id });
            _repository.Add(student);
            await _unitOfWorkManager[typeof(CustomerDbContext).FullName].CommitAsync();
            return await _repository.FindAsync(s => s.Id == student.Id);
        }

    }
}
