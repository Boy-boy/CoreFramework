using System.Diagnostics;
using System.Threading.Tasks;
using Core.EventBus.Integration;
using Core.EventBus.Local;
using Core.Uow;
using Microsoft.AspNetCore.Mvc;
using PublishApi.Event;

namespace PublishApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IIntegrationMessagePublisher _publisher;
        private readonly ILocalMessagePublisher _localPublisher;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public WeatherForecastController(
            IIntegrationMessagePublisher publisher,
            ILocalMessagePublisher localPublisher,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _publisher = publisher;
            _localPublisher = localPublisher;
            _unitOfWorkManager = unitOfWorkManager;
        }

        [HttpPost]
        public async Task<string> Post()
        {
            var sw = new Stopwatch();
            sw.Start();

            // 用 UoW 开启业务事务；publisher 检测到 outbox 上下文自动走 outbox 路径
            await using var uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions(isTransactional: true));
            for (var i = 0; i < 500; i++)
            {
                await _publisher.PublishAsync(new CustomerEvent());
                await _localPublisher.PublishAsync(new CustomerEvent());
            }
            await uow.CommitAsync();

            sw.Stop();
            return $"500个事件，耗时：{sw.ElapsedMilliseconds}";
        }
    }
}
