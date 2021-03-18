using Core.EventBus.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Core.EventBus
{
    public class EventBusBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public EventBusBackgroundService(
            IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var provider = _serviceScopeFactory.CreateScope().ServiceProvider;
            var options = provider.GetRequiredService<IOptions<EventBusOptions>>();

            //初始化订阅
            var messageSubscribe = provider.GetService<IMessageSubscribe>();
            messageSubscribe?.Initialize(options.Value.AutoRegistrarHandlersAssemblies);

            //初始化消息存储
            var storage = provider.GetService<IStorage>();
            storage?.InitializeAsync(stoppingToken);

            return Task.CompletedTask;
        }
    }
}
