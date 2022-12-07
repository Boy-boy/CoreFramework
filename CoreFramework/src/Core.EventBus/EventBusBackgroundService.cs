using Core.EventBus.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Core.EventBus.Integration;
using Core.EventBus.Local;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus
{
    public class EventBusBackgroundService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IOutBoxSender _outBoxSender;

        public EventBusBackgroundService(
            IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var provider = _serviceScopeFactory.CreateScope().ServiceProvider;
            var options = provider.GetRequiredService<IOptions<EventBusOptions>>();

            //初始化订阅
            var localMessageSubscribe = provider.GetService<ILocalMessageSubscribe>();
            var integrationMessageSubscribe = provider.GetService<IIntegrationMessageSubscribe>();
            localMessageSubscribe?.Initialize(options.Value.MessageHandlerAssemblies);
            integrationMessageSubscribe?.Initialize(options.Value.MessageHandlerAssemblies);

            //初始化消息存储
            var storage = provider.GetService<IStorage>();
            storage?.InitializeAsync(cancellationToken);

            if (integrationMessageSubscribe != null && storage != null)
            {
                //开启发件箱
                _outBoxSender = provider.GetRequiredService<IOutBoxSender>();
                await _outBoxSender.StartAsync(cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var stopAsync = _outBoxSender?.StopAsync(cancellationToken);
            if (stopAsync != null)
                await stopAsync;
        }

    }
}
