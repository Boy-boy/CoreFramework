using Core.Ddd.Domain.Repositories;
using Core.EventBus;
using EntityFrameworkCore.Api.Entities;
using System.Threading.Tasks;
using Core.Uow;

namespace EntityFrameworkCore.Api.Events
{
    public class AddLocalStudentEventHandler : IMessageHandler<AddLocalStudentEvent>
    {
        private readonly IRepository<Student> _repository;

        public AddLocalStudentEventHandler(IRepository<Student> repository)
        {
            _repository = repository;
        }

        public async Task HandAsync(AddLocalStudentEvent message)
        {
            var student = new Student("李四", 24);
            _repository.Add(student);
        }
    }

    public class AddStudentEventHandler : IMessageHandler<AddStudentEvent>
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<Student> _repository;

        public AddStudentEventHandler(IUnitOfWorkManager unitOfWorkManager,
            IRepository<Student> repository)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _repository = repository;
        }

        public async Task HandAsync(AddStudentEvent message)
        {
            var uow = _unitOfWorkManager.Begin();
            var student = new Student("李四", 24);
            _repository.Add(student);
            await uow.CommitAsync();
        }
    }
}
