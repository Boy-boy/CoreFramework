using Core.EmailClient.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.EmailClient
{
    public class EmailClientBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public EmailClientBackgroundService(
            IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var provider = _serviceScopeFactory.CreateScope().ServiceProvider;

            //初始化消息存储
            var storage = provider.GetService<IEmailStorage>();
            storage?.InitializeAsync(stoppingToken);

            return Task.CompletedTask;
        }
    }
}
