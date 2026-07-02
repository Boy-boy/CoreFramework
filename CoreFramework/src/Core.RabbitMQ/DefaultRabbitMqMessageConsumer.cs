using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.RabbitMQ
{
    internal class DefaultRabbitMqMessageConsumer : IRabbitMqMessageConsumer
    {
        private readonly ILogger<DefaultRabbitMqMessageConsumer> _logger;
        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly IOptions<RabbitMqOptions> _options;
        private readonly RabbitMqMetrics _metrics;
        private Timer _timer;

        /// <summary>
        /// 仅串行化 channel 创建 / 销毁这段"快"操作。
        /// TryConnect 的 Polly 退避(最坏取决于 <see cref="RabbitMqOptions.ConnectionRetryCount"/>)刻意放在锁外,避免 Dispose 卡在锁上。
        /// </summary>
        private readonly object _timerLock = new();
        private volatile bool _disposed;

        protected ConcurrentBag<Func<IModel, BasicDeliverEventArgs, Task>> ProcessEvents { get; }
        protected RabbitMqExchangeDeclareConfigure ExchangeDeclare { get; private set; }
        protected RabbitMqQueueDeclareConfigure QueueDeclare { get; private set; }
        protected IModel ConsumerChannel { get; private set; }

        protected ConcurrentDictionary<string, string> BindingQueueRoutingKeys { get; }

        public DefaultRabbitMqMessageConsumer(
            IRabbitMqPersistentConnection connection,
            IOptions<RabbitMqOptions> options,
            ILogger<DefaultRabbitMqMessageConsumer> logger,
            RabbitMqMetrics metrics = null)
        {
            _logger = logger;
            _persistentConnection = connection;
            _options = options;
            _metrics = metrics;
            ProcessEvents = new ConcurrentBag<Func<IModel, BasicDeliverEventArgs, Task>>();
            BindingQueueRoutingKeys = new ConcurrentDictionary<string, string>();
        }

        public void Initialize(
            RabbitMqExchangeDeclareConfigure exchangeDeclare,
            RabbitMqQueueDeclareConfigure queueDeclare)
        {
            ExchangeDeclare = exchangeDeclare;
            QueueDeclare = queueDeclare;
            TryCreateExchangeAndQueue();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            var opts = _options?.Value;
            var initialDelay = TimeSpan.FromSeconds(Math.Max(0, opts?.ConsumerInitialDelaySeconds ?? 2));
            var period = TimeSpan.FromSeconds(Math.Max(1, opts?.ConsumerRebuildIntervalSeconds ?? 30));
            _timer = new Timer(sender =>
            {
                TimerCallback();
            }, this, initialDelay, period);
        }

        private void TryCreateExchangeAndQueue()
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using var channel = _persistentConnection.CreateModel();
            ExchangeDeclare.Declare(channel);
            QueueDeclare.Declare(channel);
        }

        public void OnMessageReceived(Func<IModel, BasicDeliverEventArgs, Task> processEvent)
        {
            // 用 Delegate.Equals 同时比对 Target + Method，避免不同实例的同一方法被误判为重复后被静默丢弃。
            if (ProcessEvents.Any(p => p.Equals(processEvent)))
                return;
            ProcessEvents.Add(processEvent);
        }

        public Task BindAsync(string routingKey)
        {
            // 已绑定过的 routing key 直接跳过 —— 上层订阅器在同 messageType 有多个 handler 时
            // 会重复调用 BindAsync,先查字典避免每次都开 channel 走一遍 RPC
            if (BindingQueueRoutingKeys.ContainsKey(routingKey))
                return Task.CompletedTask;

            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueBind(queue: QueueDeclare.QueueName,
                    exchange: ExchangeDeclare.ExchangeName,
                    routingKey: routingKey);
                BindingQueueRoutingKeys.TryAdd(routingKey, QueueDeclare.QueueName);
            }
            return Task.CompletedTask;
        }

        public Task UnbindAsync(string routingKey)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: QueueDeclare.QueueName,
                    exchange: ExchangeDeclare.ExchangeName,
                    routingKey: routingKey);
                BindingQueueRoutingKeys.TryRemove(routingKey, out _);
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 先停 timer，再清 channel。Timer.Dispose(WaitHandle) 会等已排队的回调跑完，最多 5 秒。
            // TimerCallback 内的慢操作（TryConnect 退避）在锁外执行，回调出锁后会 short-circuit on _disposed。
            var timer = Interlocked.Exchange(ref _timer, null);
            if (timer != null)
            {
                using var waitHandle = new ManualResetEvent(false);
                if (timer.Dispose(waitHandle))
                {
                    waitHandle.WaitOne(TimeSpan.FromSeconds(5));
                }
            }

            // 锁里只是为了和正处在"创建 channel"窗口的 TimerCallback 互斥，不与 TryConnect 阻塞共用。
            IModel channel;
            lock (_timerLock)
            {
                channel = ConsumerChannel;
                ConsumerChannel = null;
            }

            if (channel != null)
            {
                try { channel.Close(); }
                catch (Exception ex) { _logger.LogWarning(ex, "RabbitMQ consumer channel close failed"); }

                try { channel.Dispose(); }
                catch (Exception ex) { _logger.LogWarning(ex, "RabbitMQ consumer channel dispose failed"); }
            }
        }

        private void TimerCallback()
        {
            if (_disposed) return;

            // 整段裹 try/catch：System.Threading.Timer 回调里 unobserved 异常在 .NET 5+ 默认会打崩进程。
            // 任一步骤（TryConnect/CreateModel/Declare/Bind/BasicConsume）都可能抛 broker 异常。
            try
            {
                // 慢操作放锁外：Polly 最坏 126s，不能拖住 Dispose。
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                if (_disposed) return;

                IModel newChannel = null;
                try
                {
                    lock (_timerLock)
                    {
                        if (_disposed) return;
                        if (ConsumerChannel != null && !ConsumerChannel.IsClosed) return;

                        newChannel = _persistentConnection.CreateModel();

                        // channel 重建时必须重新声明 exchange/queue 并按已记录的 routing key 重绑：
                        // 之前只 CreateModel，遇上 broker 重启或 auto-delete/非 durable 队列被回收后会 BasicConsume NOT_FOUND。
                        ExchangeDeclare.Declare(newChannel);
                        QueueDeclare.Declare(newChannel);
                        foreach (var kvp in BindingQueueRoutingKeys)
                        {
                            newChannel.QueueBind(
                                queue: kvp.Value,
                                exchange: ExchangeDeclare.ExchangeName,
                                routingKey: kvp.Key);
                        }

                        StartBasicConsume(newChannel);
                        var previous = ConsumerChannel;
                        ConsumerChannel = newChannel;
                        newChannel = null; // ownership 转移给 ConsumerChannel
                        _metrics?.RecordConsumerChannelRebuild(QueueDeclare?.QueueName, previous != null);
                    }
                }
                finally
                {
                    // 走 catch / 提前 return 时把没挂上的 channel 释放掉，避免半成品泄漏。
                    if (newChannel != null)
                    {
                        try { newChannel.Close(); } catch { }
                        try { newChannel.Dispose(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer timer callback failed; will retry on next tick");
            }
        }

        private void StartBasicConsume(IModel channel)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += Consumer_Received;
            var prefetch = _options?.Value?.ConsumerPrefetchCount ?? 30;
            if (prefetch == 0) prefetch = 30; // 0 = unlimited,禁止;显式兜底给一个合理默认
            channel.BasicQos(0, prefetch, false);
            channel.BasicConsume(
                queue: QueueDeclare.QueueName,
                autoAck: false,
                consumer: consumer);
        }

        /// <summary>
        /// 消费一条消息:每个 handler 独立 try/catch,聚合结果后再决定 ack/nack。
        /// <br/>
        /// 与旧实现的差异:旧实现 foreach 里任一 handler 抛异常都会跳出循环,后续订阅者被静默跳过。
        /// 现在每个 handler 都会跑到,失败被聚合成 <see cref="AggregateException"/> 上抛决策层,
        /// 保证同 routing key 的其他订阅者不被前一个失败者拖累。
        /// </summary>
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var asyncEventingBasicConsumer = sender as AsyncEventingBasicConsumer;
            var model = asyncEventingBasicConsumer?.Model;
            var handlerFailures = new List<Exception>();
            var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();

            foreach (var processEvent in ProcessEvents)
            {
                try
                {
                    await processEvent(model, eventArgs);
                }
                catch (Exception ex)
                {
                    handlerFailures.Add(ex);
                    _logger.LogError(ex,
                        "RabbitMQ handler 异常 deliveryTag={DeliveryTag} routingKey={RoutingKey} redelivered={Redelivered} handler={Handler}",
                        eventArgs.DeliveryTag, eventArgs.RoutingKey, eventArgs.Redelivered,
                        processEvent.Method?.DeclaringType?.FullName + "." + processEvent.Method?.Name);
                }
            }

            var elapsedMs = (System.Diagnostics.Stopwatch.GetTimestamp() - startedAt) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            var processed = handlerFailures.Count == 0;
            Exception aggregatedFailure = handlerFailures.Count switch
            {
                0 => null,
                1 => handlerFailures[0],
                _ => new AggregateException("RabbitMQ 多个 handler 同时失败", handlerFailures),
            };

            _metrics?.RecordConsumerHandled(QueueDeclare?.QueueName, eventArgs.RoutingKey, processed, elapsedMs);

            try
            {
                if (processed)
                {
                    model?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                    return;
                }

                // 失败路径:按 FailureBehavior 决定 ack / nack。
                //   AlwaysAck       —— 与旧版兼容,失败也 ack,完全依赖上层 inbox 兜底
                //   NackNoRequeue   —— 失败立即不重投,进 DLX 或丢弃
                //   RequeueOnce(默认)—— 首次失败 requeue,二次失败 nack 不重投,配合上层 inbox 去重达成最终一致
                var behavior = _options?.Value?.FailureBehavior ?? RabbitMqFailureBehavior.RequeueOnce;
                switch (behavior)
                {
                    case RabbitMqFailureBehavior.AlwaysAck:
                        model?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                        break;
                    case RabbitMqFailureBehavior.NackNoRequeue:
                        model?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                        break;
                    case RabbitMqFailureBehavior.RequeueOnce:
                    default:
                        var requeue = !eventArgs.Redelivered;
                        model?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: requeue);
                        if (!requeue)
                        {
                            _logger.LogWarning(aggregatedFailure,
                                "RabbitMQ message 二次失败,放弃重投(可能丢失) deliveryTag={DeliveryTag} routingKey={RoutingKey}",
                                eventArgs.DeliveryTag, eventArgs.RoutingKey);
                        }
                        break;
                }
            }
            catch (Exception ackEx)
            {
                _logger.LogWarning(ackEx, "RabbitMQ ack/nack 调用失败 deliveryTag={DeliveryTag}", eventArgs.DeliveryTag);
            }
        }

        public bool HasAnyRoutingKey()
        {
            return BindingQueueRoutingKeys.Any();
        }
    }
}
