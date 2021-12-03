using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.EventBus
{
    public class DefaultMessageMailBox : IMessageMailBox
    {
        private readonly ILogger<DefaultMessageMailBox> _logger;
        private readonly IMessagePublisher _publisher;
        private readonly ConcurrentQueue<IMessage> _queue;
        private readonly object _lock = new();

        public bool IsRunning { get; private set; }

        public DefaultMessageMailBox(IServiceScopeFactory serviceScopeFactory,
            ILogger<DefaultMessageMailBox> logger)
        {
            _logger = logger;
            var provider = serviceScopeFactory.CreateScope().ServiceProvider;
            _publisher = provider.GetService<IMessagePublisher>();
            _queue = new ConcurrentQueue<IMessage>();
        }

        public void EnqueueMessage(IMessage message)
        {
            lock (_lock)
            {
                _queue.Enqueue(message);
                TryRun();
            }
        }

        private void TryRun()
        {
            if (IsRunning)
                return;
            lock (_lock)
            {
                if (IsRunning)
                    return;
                IsRunning = true;
                Task.Factory.StartNew(ProcessMessages);
            }
        }

        public void CompleteRun()
        {
            lock (_lock)
            {
                IsRunning = false;
                if (_queue.Any())
                {
                    TryRun();
                }
            }
        }

        private async Task ProcessMessages()
        {
            try
            {
                if (_queue.TryDequeue(out var message))
                {
                    if (_publisher == null)
                        return;
                    await ((MessagePublisherBase)_publisher).SendAsync(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{GetType().Name} run has unknown exception");
            }
            finally
            {
                CompleteRun();
            }
        }
    }
}
