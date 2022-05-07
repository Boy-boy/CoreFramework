using Core.EmailClient.Storage;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Polly;

namespace Core.EmailClient
{
    public class EmailSendMailBox
    {
        private readonly ILogger<EmailSendMailBox> _logger;
        private readonly IEmailClient _emailClient;
        private readonly IEmailStorage _emailStorage;
        private readonly ConcurrentQueue<MailBoxMessage> _queue;
        private readonly object _lock = new();
        private readonly int _retryCount = 3;

        public bool IsRunning { get; private set; }

        public EmailSendMailBox(IEmailClient emailClient,
            ILogger<EmailSendMailBox> logger,
            IEmailStorage emailStorage = null)
        {
            _logger = logger;
            _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
            _emailStorage = emailStorage;
            _queue = new ConcurrentQueue<MailBoxMessage>();
        }

        public void EnqueueMessage(MailBoxMessage message)
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
                var policy = Policy.Handle<Exception>()
                    .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(retryAttempt, 2)), (ex, time) =>
                    {
                        _logger.LogWarning(ex, $"failed to send email,after {time.TotalSeconds}s ({ex.Message})");
                    });

                while (_queue.TryDequeue(out var message))
                {
                    await policy.ExecuteAsync(async () =>
                    {
                        await ((EmailClientBase)_emailClient).PushAsync(message.MailBodyEntity);
                        if (_emailStorage != null)
                            await _emailStorage.UpdateAsync(new Storage.Model.UpdateEmailModel(message.Id, true));
                    });
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

    public class MailBoxMessage
    {
        public MailBoxMessage(string id, MailBodyEntity mailBodyEntity)
        {
            Id = id;
            MailBodyEntity = mailBodyEntity;
        }

        /// <summary>
        /// 邮箱持久化数据id
        /// </summary>
        public string Id { get; set; }

        public MailBodyEntity MailBodyEntity { get; set; }
    }
}
