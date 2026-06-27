using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.RabbitMQ
{
    public class DefaultRabbitMqMessageConsumerManager : IRabbitMqMessageConsumerManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DefaultRabbitMqMessageConsumerManager> _logger;

        /// <summary>
        /// 用 (exchange, queue) tuple 做 key，避免之前 <c>$"{exchange}_{queue}"</c> 拼字符串的碰撞
        /// （e.g. "a_b"+"c" 与 "a"+"b_c" 会撞）。
        /// </summary>
        private readonly ConcurrentDictionary<(string Exchange, string Queue), IRabbitMqMessageConsumer> _consumers
            = new();

        /// <summary>
        /// 直接持根 <see cref="IServiceProvider"/>。<see cref="DefaultRabbitMqMessageConsumer"/> 的依赖
        /// （<see cref="IRabbitMqPersistentConnection"/>、ILogger）都是 Singleton，从根 provider 解析安全；
        /// 不需要创建 scope（之前的实现 <c>CreateScope()</c> 后只取 <c>ServiceProvider</c>，scope 永不 dispose → 资源泄漏）。
        /// </summary>
        public DefaultRabbitMqMessageConsumerManager(
            IServiceProvider serviceProvider,
            ILogger<DefaultRabbitMqMessageConsumerManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IRabbitMqMessageConsumer TryCreate(RabbitMqExchangeDeclareConfigure exchangeDeclare,
            RabbitMqQueueDeclareConfigure queueDeclare)
        {
            if (exchangeDeclare == null)
                throw new ArgumentNullException(nameof(exchangeDeclare));

            if (queueDeclare == null)
                throw new ArgumentNullException(nameof(queueDeclare));

            var key = (exchangeDeclare.ExchangeName, queueDeclare.QueueName);

            // GetOrAdd 不能直接给 factory 用，因为 factory 在并发下可能跑多次，会创建多余的 consumer
            // （并真的 Initialize 起 timer/channel）然后被丢弃，造成连接泄漏。所以用 try-get + add 一遍。
            if (_consumers.TryGetValue(key, out var consumer))
                return consumer;

            var created = Create(exchangeDeclare, queueDeclare);
            if (_consumers.TryAdd(key, created))
                return created;

            // 罕见竞态：另一个线程刚塞了一个 consumer 进去，本线程的多余 instance 释放掉。
            try { created.Dispose(); }
            catch (Exception ex) { _logger.LogWarning(ex, "RabbitMQ consumer dispose failed during create race"); }
            return _consumers[key];
        }

        public bool TryGet(string exchangeName, string queueName, out IRabbitMqMessageConsumer consumer)
        {
            return _consumers.TryGetValue((exchangeName, queueName), out consumer);
        }

        /// <summary>
        /// 移除并 Dispose。之前实现只从字典移除，timer/channel 还在跑，
        /// 调用方忘记先 Dispose 就泄漏；现在统一由 manager 兜底释放。重复 Dispose 安全（consumer 内部有 _disposed 守卫）。
        /// </summary>
        public bool TryRemove(string exchangeName, string queueName)
        {
            if (!_consumers.TryRemove((exchangeName, queueName), out var consumer))
                return false;

            try { consumer.Dispose(); }
            catch (Exception ex) { _logger.LogWarning(ex, "RabbitMQ consumer dispose failed during TryRemove"); }
            return true;
        }

        private IRabbitMqMessageConsumer Create(RabbitMqExchangeDeclareConfigure exchangeDeclare,
            RabbitMqQueueDeclareConfigure queueDeclare)
        {
            var consumer = (DefaultRabbitMqMessageConsumer)ActivatorUtilities.CreateInstance(
                _serviceProvider,
                typeof(DefaultRabbitMqMessageConsumer));
            consumer.Initialize(exchangeDeclare, queueDeclare);
            return consumer;
        }
    }
}
