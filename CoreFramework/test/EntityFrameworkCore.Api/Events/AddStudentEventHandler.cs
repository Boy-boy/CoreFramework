using Core.Ddd.Domain.Repositories;
using Core.EventBus;
using EntityFrameworkCore.Api.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Api.Events
{
    // 消费 handler 不再需要自己管 UoW / 幂等：
    // 注册了 AddEfCoreEventBusStorage 后，InboxAwareMessageHandlerInvoker 会为每次调用
    //   ① 新建 DI scope    ② 开启 transactional UoW
    //   ③ 写 inbox 行（去重） ④ 调用 handler  ⑤ commit / rollback。
    public class AddLocalStudentEventHandler : IMessageHandler<AddLocalStudentEvent>
    {
        private readonly IRepository<Student> _repository;

        public AddLocalStudentEventHandler(IRepository<Student> repository)
        {
            _repository = repository;
        }

        public async Task HandAsync(AddLocalStudentEvent message, CancellationToken cancellationToken = default)
        {
            var student = new Student("李四", 24);
            await _repository.AddAsync(student);
        }
    }

    public class AddStudentEventHandler : IMessageHandler<AddStudentEvent>
    {
        private readonly IRepository<Student> _repository;

        public AddStudentEventHandler(IRepository<Student> repository)
        {
            _repository = repository;
        }

        public async Task HandAsync(AddStudentEvent message, CancellationToken cancellationToken = default)
        {
            var student = new Student("李四", 24);
            await _repository.AddAsync(student);
        }
    }
}
