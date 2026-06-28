using System.Diagnostics;
using System.Threading.Tasks;
using Core.EventBus.Integration;
using Core.EventBus.Local;
using Microsoft.AspNetCore.Mvc;
using PublishApi.Event;

namespace PublishApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IIntegrationPublisher _publisher;
        private readonly ILocalPublisher _localPublisher;

        public WeatherForecastController(
            IIntegrationPublisher publisher,
            ILocalPublisher localPublisher)
        {
            _publisher = publisher;
            _localPublisher = localPublisher;
        }

        [HttpPost]
        public async Task<string> Post()
        {
            var sw = new Stopwatch();
            sw.Start();

            // 直发场景:不开 UoW,publisher 检测不到 ambient outbox 上下文,直接打 broker。
            // 如需 outbox 模式,请在 StartupModule 中追加 CoreEventBusEfCoreStorageModule 并配置 DbContext,
            // 然后再用 IUnitOfWorkManager 包住 publish 循环。
            for (var i = 0; i < 500; i++)
            {
                await _publisher.PublishAsync(new CustomerEvent());
                await _localPublisher.PublishAsync(new CustomerEvent());
            }

            sw.Stop();
            return $"500个事件，耗时：{sw.ElapsedMilliseconds}";
        }
    }
}
