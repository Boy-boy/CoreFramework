using Core.EventBus.Storage;
using Core.Infrastructure.Timer;
using Core.Json.Newtonsoft;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Integration
{
    public class OutBoxSender : IOutBoxSender
    {
        private readonly IStorage _storage;
        private readonly IIntegrationMessagePublisher _publisher;
        private readonly AsyncTimer _timer;

        public OutBoxSender(IServiceProvider serviceProvider)
        {
            _publisher = serviceProvider.GetRequiredService<IIntegrationMessagePublisher>();
            _storage = serviceProvider.GetRequiredService<IStorage>();

            _timer = new AsyncTimer
            {
                Period = Convert.ToInt32(TimeSpan.FromSeconds(2).TotalMilliseconds)
            };
            _timer.Elapsed += TimerOnElapsed;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer?.Start(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop(cancellationToken);
            return Task.CompletedTask;
        }

        private async Task TimerOnElapsed(AsyncTimer arg)
        {
            await RunAsync();
        }

        private async Task RunAsync()
        {
            var messages = _storage.GetMessages(1000);
            foreach (var message in messages)
            {
                var messageType = Assembly.Load(message.AssemblyName).GetType(message.MessageName);
                var @event = message.MessageData.ToObject(messageType) as IMessage;
                await _publisher.PublishAsync(@event);
                _storage.Delete(message.Id);
            }
        }
    }
}
