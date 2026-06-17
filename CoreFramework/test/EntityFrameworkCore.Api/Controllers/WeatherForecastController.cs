using Core.Ddd.Domain.Repositories;
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
        [UnitOfWork]
        public async Task<List<Student>> Get()
        {
            return await _repository.FindAllAsync(s => true);
        }

        [HttpPost("add")]
        [UnitOfWork(isTransactional: true)]
        public async Task<List<Student>> Add()
        {
            await using var uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions(isTransactional: true));
            var student = new Student("张三", 24);
            student.AddLocalEvent(new AddLocalStudentEvent { AggregateRootId = student.Id });
            student.AddDistributedEvent(new AddStudentEvent { AggregateRootId = student.Id });
            await _repository.AddAsync(student);
            await uow.CommitAsync();
            return await _repository.FindAllAsync(x => true);
        }
    }
}
