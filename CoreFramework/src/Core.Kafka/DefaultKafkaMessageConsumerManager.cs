using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Kafka
{
    /// <summary>
    /// <see cref="IKafkaMessageConsumerManager"/> 默认实现。
    /// 一个 groupId 对应一个 consumer 实例，被多次 TryCreate 复用。
    /// </summary>
    public class DefaultKafkaMessageConsumerManager : IKafkaMessageConsumerManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly KafkaOptions _options;
        private readonly ILogger<DefaultKafkaMessageConsumerManager> _logger;
        private readonly ConcurrentDictionary<string, IKafkaMessageConsumer> _consumers;
        private readonly object _lock = new();

        /// <summary>AdminClient.CreateTopicsAsync 的等待上限。broker 不可达时避免启动期被卡到默认 60 秒。</summary>
        private static readonly TimeSpan TopicDeclareTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 直接持根 <see cref="IServiceProvider"/>，<see cref="DefaultKafkaMessageConsumer"/> 的依赖
        /// （<see cref="KafkaOptions"/>、ILogger）都是 Singleton-friendly，无需创建 scope；
        /// 之前用 <c>IServiceScopeFactory.CreateScope().ServiceProvider</c> 会留下永远不 dispose 的 scope，造成资源泄漏。
        /// </summary>
        public DefaultKafkaMessageConsumerManager(
            IServiceProvider serviceProvider,
            IOptions<KafkaOptions> options,
            ILogger<DefaultKafkaMessageConsumerManager> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
            _consumers = new ConcurrentDictionary<string, IKafkaMessageConsumer>();
        }

        public IKafkaMessageConsumer TryCreate(string groupId, KafkaTopicDeclareConfigure topicDeclare = null)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("groupId is required", nameof(groupId));

            // topic 显式声明对 consumer 实例的 key 没有影响：相同 groupId 只建一次 consumer
            if (topicDeclare != null)
                TryDeclareTopic(topicDeclare);

            lock (_lock)
            {
                if (_consumers.TryGetValue(groupId, out var consumer))
                    return consumer;

                consumer = Create(groupId);
                _consumers.TryAdd(groupId, consumer);
                return consumer;
            }
        }

        public bool TryGet(string groupId, out IKafkaMessageConsumer consumer)
        {
            lock (_lock)
            {
                return _consumers.TryGetValue(groupId, out consumer);
            }
        }

        public bool TryRemove(string groupId)
        {
            lock (_lock)
            {
                return _consumers.TryRemove(groupId, out _);
            }
        }

        private IKafkaMessageConsumer Create(string groupId)
        {
            var consumer = (DefaultKafkaMessageConsumer)ActivatorUtilities.CreateInstance(
                _serviceProvider,
                typeof(DefaultKafkaMessageConsumer));
            consumer.Initialize(groupId);
            return consumer;
        }

        /// <summary>
        /// 用 AdminClient 显式创建 topic。已存在静默忽略；broker 不可达 / 超时 / 权限不足等
        /// 一律降级为 warning，并依赖 broker 的 <c>auto.create.topics.enable</c> 兜底。
        /// </summary>
        private void TryDeclareTopic(KafkaTopicDeclareConfigure topicDeclare)
        {
            try
            {
                using var admin = new AdminClientBuilder(_options.Connection.BuildClientConfig()).Build();
                var spec = new TopicSpecification
                {
                    Name = topicDeclare.TopicName,
                    NumPartitions = topicDeclare.NumPartitions,
                    ReplicationFactor = topicDeclare.ReplicationFactor,
                    Configs = topicDeclare.Configs is Dictionary<string, string> d
                        ? d
                        : new Dictionary<string, string>(topicDeclare.Configs),
                };
                var createOptions = new CreateTopicsOptions
                {
                    // 启动期能容忍的最长等待：broker 不可达时绝对不该卡数十秒
                    OperationTimeout = TopicDeclareTimeout,
                    RequestTimeout = TopicDeclareTimeout,
                };
                admin.CreateTopicsAsync(new[] { spec }, createOptions).GetAwaiter().GetResult();
                _logger.LogInformation("Kafka topic created: {Topic}", topicDeclare.TopicName);
            }
            catch (CreateTopicsException ex)
                when (ex.Results.Count > 0 && ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
            {
                _logger.LogTrace("Kafka topic already exists: {Topic}", topicDeclare.TopicName);
            }
            catch (Exception ex)
            {
                // broker 不可达 / timeout / 权限不足都走这里。不致命 —— 信任 broker 的 auto.create
                _logger.LogWarning(ex, "Kafka topic declare failed (will rely on broker auto-create): {Topic}",
                    topicDeclare.TopicName);
            }
        }
    }
}
