using Core.Ddd.Domain.Repositories;
using Core.EventBus;
using EntityFrameworkCore.Api.Entities;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Api.Events
{
    public class AddStudentEventHandler : IMessageHandler<AddStudentEvent>
    {
        private readonly IRepository<Student> _repository;

        public AddStudentEventHandler(IRepository<Student> repository)
        {
            _repository = repository;
        }

        public async Task HandAsync(AddStudentEvent message)
        {
            var student = new Student("李四", 24);
            _repository.Add(student);
        }
    }
}
