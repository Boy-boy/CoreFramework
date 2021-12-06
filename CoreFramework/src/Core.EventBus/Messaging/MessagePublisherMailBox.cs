using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.EventBus
{
    public class MessagePublisherMailBox
    {
        private readonly ILogger<MessagePublisherMailBox> _logger;
        private readonly IMessagePublisher _publisher;
        private readonly ConcurrentQueue<IMessage> _queue;
        private readonly object _lock = new();

        public bool IsRunning { get; private set; }

        public MessagePublisherMailBox(IMessagePublisher messagePublisher,
            ILogger<MessagePublisherMailBox> logger)
        {
            _logger = logger;
            _publisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
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
