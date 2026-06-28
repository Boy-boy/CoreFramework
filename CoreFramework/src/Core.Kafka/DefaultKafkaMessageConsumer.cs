using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Kafka
{
    /// <summary>
    /// <see cref="IKafkaMessageConsumer"/> 默认实现，一个实例对应一个 consumer group。
    /// </summary>
    /// <remarks>
    /// <para><b>关键 consumer 配置</b></para>
    /// <list type="bullet">
    ///   <item><description><c>EnableAutoCommit = false</c> + <c>EnableAutoOffsetStore = false</c>：
    ///   手动控制 commit 时机，配合 <see cref="PollLoop"/> 的"每条消息处理完都 commit"策略实现至少一次语义。</description></item>
    ///   <item><description><c>AutoOffsetReset = Earliest</c>：首次 join group 时从最早 offset 读，避免上线前事件被静默吞掉。</description></item>
    /// </list>
    ///
    /// <para><b>失败处理策略(让 broker 在下一轮重投同条消息)</b></para>
    /// <para>
    /// <see cref="PollLoop"/> 对每条消息都先调 handler 集合,只要任一 handler 抛异常,
    /// 就跳过 commit 并 <see cref="Confluent.Kafka.IConsumer{TKey,TValue}.Seek"/> 回当前 <see cref="ConsumeResult{TKey, TValue}.TopicPartitionOffset"/>,
    /// 然后按 <see cref="KafkaOptions.FailureBackoff"/> 退避一段后继续 PollLoop ——
    /// 下一轮 <see cref="Confluent.Kafka.IConsumer{TKey,TValue}.Consume"/> 会重投同条消息,与上层 inbox 去重协同达成"业务最终一致"。
    /// </para>
    /// <para>
    /// <b>为什么必须 Seek 不能只"跳过 commit"</b>:librdkafka 内部 cursor 在 <c>Consume</c> 返回时已经前进,
    /// broker 端 offset 即使不动,consumer 进程的下一次 <c>Consume</c> 仍会拉下一条。Seek 强制把内部 cursor 拨回。
    /// </para>
    /// <para>
    /// <b>poison message 截断口</b>:同条 offset 连续失败次数达到 <see cref="KafkaOptions.MaxConsecutiveFailures"/>(默认 5)
    /// 时,框架 commit 跳过该条 + LogWarning,避免单条坏消息永久阻塞整个 partition。再精细的"可观测丢失"
    /// (如转 dead-letter topic) 由业务层在 inbox 跟踪 <c>(messageId, handlerType)</c> 失败计数后自行实现。
    /// </para>
    /// </remarks>
    internal class DefaultKafkaMessageConsumer : IKafkaMessageConsumer
    {
        private readonly KafkaOptions _options;
        private readonly ILogger<DefaultKafkaMessageConsumer> _logger;
        private readonly ConcurrentBag<Func<IConsumer<string, byte[]>, ConsumeResult<string, byte[]>, Task>> _processEvents;
        private readonly ConcurrentDictionary<string, byte> _subscribedTopics;
        private readonly object _pollStartLock = new();
        private readonly CancellationTokenSource _cts = new();

        private string _groupId;
        private IConsumer<string, byte[]> _consumer;
        private Task _pollLoop;
        private bool _disposed;

        /// <summary>
        /// 订阅集"有变更待应用"的位。SubscribeTopicAsync / UnsubscribeTopicAsync 只翻这个位,
        /// 真正的 <c>_consumer.Subscribe/Unsubscribe</c> 调用由 <see cref="PollLoop"/> 在同一线程上执行 ——
        /// Confluent.Kafka 的 consumer 文档明确禁止 Subscribe 与 Consume 跨线程并发。
        /// </summary>
        private int _subscriptionDirty;

        /// <summary>
        /// 最近一条失败消息的 TopicPartitionOffset,与 <see cref="_consecutiveFailures"/> 配合
        /// 实现 <see cref="KafkaOptions.MaxConsecutiveFailures"/> 截断。仅 PollLoop 线程访问,无需锁。
        /// </summary>
        private TopicPartitionOffset _lastFailedOffset;

        /// <summary>同条 offset 连续失败的次数。仅 PollLoop 线程访问,无需锁。</summary>
        private int _consecutiveFailures;

        /// <summary>poll 间隔下限：有 topic 订阅时用，broker 推消息时几乎不会真正等满</summary>
        private static readonly TimeSpan ActivePollInterval = TimeSpan.FromSeconds(1);

        /// <summary>空订阅时的 sleep 间隔：避免对一个空 consumer 反复 syscall 拉空结果</summary>
        private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(5);

        /// <summary>PollLoop 内非预期异常的退避间隔：避免热循环刷爆日志</summary>
        private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(1);

        public DefaultKafkaMessageConsumer(
            IOptions<KafkaOptions> options,
            ILogger<DefaultKafkaMessageConsumer> logger)
        {
            _options = options.Value;
            _logger = logger;
            _processEvents = new ConcurrentBag<Func<IConsumer<string, byte[]>, ConsumeResult<string, byte[]>, Task>>();
            _subscribedTopics = new ConcurrentDictionary<string, byte>();
        }

        /// <summary>
        /// 初始化 consumer（绑定 group.id），但不立即启动 poll —— 启动期还要继续追加 topic 订阅，
        /// 等 <see cref="DefaultKafkaMessageConsumerManager"/> 调用 <see cref="EnsurePollStarted"/> 才正式跑起来。
        /// </summary>
        public void Initialize(string groupId)
        {
            _groupId = groupId;
            var baseConfig = _options.Connection.BuildClientConfig();
            var consumerConfig = new ConsumerConfig(baseConfig)
            {
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false,
                // 让 broker 在重平衡时尽量保留之前的 partition 分配，缩短停顿
                PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,
                EnablePartitionEof = false,
            };

            _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig)
                .SetErrorHandler((_, e) =>
                    _logger.LogWarning("Kafka consumer error: group={Group} code={Code} reason={Reason} fatal={Fatal}",
                        groupId, e.Code, e.Reason, e.IsFatal))
                .SetLogHandler((_, m) =>
                    _logger.LogTrace("Kafka consumer log: group={Group} {Facility} {Message}",
                        groupId, m.Facility, m.Message))
                .Build();
        }

        public Task SubscribeTopicAsync(string topic)
        {
            if (_subscribedTopics.TryAdd(topic, 0))
            {
                Interlocked.Exchange(ref _subscriptionDirty, 1);
                EnsurePollStarted();
            }
            return Task.CompletedTask;
        }

        public Task UnsubscribeTopicAsync(string topic)
        {
            if (_subscribedTopics.TryRemove(topic, out _))
            {
                Interlocked.Exchange(ref _subscriptionDirty, 1);
            }
            return Task.CompletedTask;
        }

        public bool HasAnyTopic() => !_subscribedTopics.IsEmpty;

        public void OnMessageReceived(Func<IConsumer<string, byte[]>, ConsumeResult<string, byte[]>, Task> processEvent)
        {
            // 用 Delegate.Equals 同时比对 Target + Method，避免不同实例的同一方法被误判为重复后被静默丢弃。
            if (_processEvents.Any(p => p.Equals(processEvent)))
                return;
            _processEvents.Add(processEvent);
        }

        /// <summary>
        /// 由 <see cref="PollLoop"/> 在自己的线程上调用 —— Subscribe/Unsubscribe 与 Consume 同源,避免跨线程操作 librdkafka consumer。
        /// </summary>
        private void ApplySubscriptionOnPollThread()
        {
            try
            {
                if (_consumer == null) return;
                var topics = _subscribedTopics.Keys.ToArray();
                if (topics.Length == 0)
                {
                    _consumer.Unsubscribe();
                }
                else
                {
                    _consumer.Subscribe(topics);
                }
            }
            catch (Exception ex)
            {
                // Subscribe/Unsubscribe 抛异常(典型场景:metadata fetch timeout、临时 broker 不可达)时,
                // 调用方在 PollLoop 顶部已经把 dirty 位 Exchange 到 0 → 不回设的话一次临时故障就让订阅永远没真正生效。
                // 用 Exchange 与上面的清零形成对偶,防止与 SubscribeTopicAsync 并发翻 dirty 位时丢更新。
                Interlocked.Exchange(ref _subscriptionDirty, 1);
                _logger.LogWarning(ex,
                    "Kafka apply subscription failed; will retry on next poll: group={Group}",
                    _groupId);
            }
        }

        /// <summary>
        /// 显式按字段比对 TopicPartitionOffset。Confluent.Kafka 的 <c>Equals</c> 实现已经做了同样的事,
        /// 但代码里走显式比对避免对 SDK 实现细节的隐式依赖,也方便 future 切换类型。null 比对返回 false。
        /// </summary>
        private static bool IsSameOffset(TopicPartitionOffset a, TopicPartitionOffset b)
        {
            if (a == null || b == null) return false;
            return a.Topic == b.Topic
                && a.Partition == b.Partition
                && a.Offset == b.Offset;
        }

        private void EnsurePollStarted()
        {
            if (_pollLoop != null) return;
            lock (_pollStartLock)
            {
                if (_pollLoop != null) return;
                // Task.Factory.StartNew + async 方法返回的是 Task<Task>;Dispose 等的若是外层,只等到首个 await。
                // 用 Unwrap 拿到真正的 inner Task,让 Dispose 的 Wait 能等到 PollLoop 真正 finally 出。
                // 保留 LongRunning 提示:Consume() 是阻塞调用,占线程时长不可控,不应抢线程池工作线程
                _pollLoop = Task.Factory.StartNew(PollLoop, _cts.Token,
                        TaskCreationOptions.LongRunning, TaskScheduler.Default)
                    .Unwrap();
            }
        }

        private async Task PollLoop()
        {
            _logger.LogInformation("Kafka consumer poll loop started: group={Group}", _groupId);
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        // 把"挂账"的订阅变更在 poll 线程上一次性应用,与下面的 Consume 调用是同源,避免跨线程访问 consumer
                        if (Interlocked.Exchange(ref _subscriptionDirty, 0) != 0)
                        {
                            ApplySubscriptionOnPollThread();
                        }

                        // 空订阅时不真去 broker pull，避免 librdkafka 反复 metadata 询问 + 日志噪音
                        if (_subscribedTopics.IsEmpty)
                        {
                            await Task.Delay(IdlePollInterval, _cts.Token).ConfigureAwait(false);
                            continue;
                        }

                        ConsumeResult<string, byte[]> result;
                        try
                        {
                            result = _consumer.Consume(ActivePollInterval);
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogWarning(ex, "Kafka consume failure: group={Group}", _groupId);
                            await Task.Delay(ErrorBackoff, _cts.Token).ConfigureAwait(false);
                            continue;
                        }

                        if (result == null) continue;

                        // 任一 processEvent 失败都让本条消息走"不 commit + Seek 回 offset"路径,
                        // 由 broker 在下一轮重投同条消息;配合上层 inbox 去重达成"业务最终一致"。
                        // 仅"不 commit"不够:librdkafka 内部 cursor 已经因为本次 Consume 前进,
                        // broker 端 offset 未变也不会让 consumer 自己倒回。必须 Seek 才能强制重投。
                        bool anyFailed = false;
                        foreach (var processEvent in _processEvents)
                        {
                            try
                            {
                                await processEvent(_consumer, result).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex,
                                    "Kafka message processing failure: group={Group} topic={Topic} partition={Partition} offset={Offset}",
                                    _groupId, result.Topic, result.Partition.Value, result.Offset.Value);
                                anyFailed = true;
                            }
                        }

                        if (anyFailed)
                        {
                            // 计数同条 offset 上的连续失败:命中 MaxConsecutiveFailures 时跳过该消息推进 offset,
                            // 避免 poison message 永久阻塞 partition。
                            if (IsSameOffset(_lastFailedOffset, result.TopicPartitionOffset))
                            {
                                _consecutiveFailures++;
                            }
                            else
                            {
                                _lastFailedOffset = result.TopicPartitionOffset;
                                _consecutiveFailures = 1;
                            }

                            var max = _options.MaxConsecutiveFailures;
                            if (max > 0 && _consecutiveFailures >= max)
                            {
                                _logger.LogWarning(
                                    "Kafka 消息连续失败 {Count} 次,放弃重试 commit 跳过 group={Group} topic={Topic} partition={Partition} offset={Offset}",
                                    _consecutiveFailures, _groupId, result.Topic, result.Partition.Value, result.Offset.Value);
                                _lastFailedOffset = null;
                                _consecutiveFailures = 0;
                                try
                                {
                                    _consumer.StoreOffset(result);
                                    _consumer.Commit(result);
                                }
                                catch (KafkaException ex)
                                {
                                    _logger.LogWarning(ex,
                                        "Kafka commit-after-give-up failure: group={Group} topic={Topic}",
                                        _groupId, result.Topic);
                                }
                                continue;
                            }

                            try
                            {
                                _consumer.Seek(result.TopicPartitionOffset);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex,
                                    "Kafka seek failed after handler failure: group={Group} topic={Topic} offset={Offset}",
                                    _groupId, result.Topic, result.Offset.Value);
                            }

                            // 退避:避免 poison message 在 PollLoop 上热循环刷爆日志。
                            // 取消信号触发的 OperationCanceledException 由外层 catch 处理,这里不重复 swallow。
                            await Task.Delay(_options.FailureBackoff, _cts.Token).ConfigureAwait(false);
                            continue;   // 跳过下面的 StoreOffset/Commit
                        }

                        // 成功路径:本条 offset 已通过,重置计数(只在它与上次失败 offset 一致时,
                        // 否则可能误清 partition 重平衡 / 下条新消息的状态)。
                        if (IsSameOffset(_lastFailedOffset, result.TopicPartitionOffset))
                        {
                            _lastFailedOffset = null;
                            _consecutiveFailures = 0;
                        }

                        try
                        {
                            _consumer.StoreOffset(result);
                            _consumer.Commit(result);
                        }
                        catch (KafkaException ex)
                        {
                            _logger.LogWarning(ex, "Kafka commit failure: group={Group} topic={Topic}",
                                _groupId, result.Topic);
                        }
                    }
                    catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                    {
                        // 正常退出 —— while 顶部会因 IsCancellationRequested 退出
                    }
                    catch (Exception ex)
                    {
                        // 非预期异常（比如 consumer fatal 之外的 InvalidOperationException）：
                        // 之前的实现整个 PollLoop 退出，consumer 永久停摆。改为退避后继续，保证循环存活。
                        _logger.LogError(ex,
                            "Kafka poll iteration failed unexpectedly; retrying after backoff: group={Group}",
                            _groupId);
                        try
                        {
                            await Task.Delay(ErrorBackoff, _cts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { /* 关闭中 */ }
                    }
                }
            }
            finally
            {
                _logger.LogInformation("Kafka consumer poll loop exited: group={Group}", _groupId);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _cts.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka consumer cancel failed: group={Group}", _groupId);
            }

            // PollLoop 等待与 Close/Dispose 分开 try：即使 Wait 抛 AggregateException，资源也要释放。
            try
            {
                _pollLoop?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka poll loop wait failed: group={Group}", _groupId);
            }

            try
            {
                _consumer?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka consumer close failed: group={Group}", _groupId);
            }

            try
            {
                _consumer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka consumer dispose failed: group={Group}", _groupId);
            }

            try
            {
                _cts.Dispose();
            }
            catch { /* 容忍 */ }
        }
    }
}
