using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Core.RabbitMQ
{
    /// <summary>
    /// publisher 复用的 RabbitMQ channel 池。
    /// </summary>
    /// <remarks>
    /// <para><b>动机</b></para>
    /// <para>
    /// "每条消息开/关一条 channel"等价于每发一条都要做一次 ExchangeDeclare + ConfirmSelect +
    /// (BasicReturn 监听挂载)+ Dispose,broker 端 channel 计数飙升、网络 round-trip 严重影响吞吐。
    /// 本池把"每 channel 一次性"的工作集中到首次创建时做一次,后续 publish 只走纯净的
    /// BasicPublish + WaitForConfirms。
    /// </para>
    ///
    /// <para><b>并发模型</b></para>
    /// <list type="bullet">
    ///   <item><description>RabbitMQ.Client v6 的 channel(IModel)<b>不是线程安全的</b>,publish 期间必须独占。</description></item>
    ///   <item><description>用 <see cref="SemaphoreSlim"/> 限制同时持有 channel 的线程数 = <see cref="_maxSize"/>。</description></item>
    ///   <item><description>用 <see cref="ConcurrentQueue{T}"/> 管理空闲 channel;同一时刻不会有两个线程拿到同一条。</description></item>
    /// </list>
    ///
    /// <para><b>连接断开 / channel 失效</b></para>
    /// <para>
    /// Acquire 时若取到的 channel 已关闭(broker 重启 / 网络抖动 → 自动恢复时 channel 不会跟着续命),
    /// 池里直接释放它并新建一条;Return 时也会判定 IsOpen,关闭的 channel 不再回收。
    /// </para>
    /// </remarks>
    public sealed class RabbitMqPublishChannelPool : IRabbitMqPublishChannelPool
    {
        private readonly IRabbitMqPersistentConnection _connection;
        private readonly string _exchangeName;
        private readonly int _maxSize;
        private readonly ILogger _logger;
        private readonly ConcurrentQueue<IModel> _idle = new();
        private readonly SemaphoreSlim _slots;
        /// <summary>
        /// 每条 channel 最近一次 BasicReturn 事件。Acquire 时清,WaitForConfirmsOrThrow 时消费。
        /// 用 ConcurrentDictionary 兼容 BasicReturn 在 channel IO 线程异步 fire 的语义。
        /// </summary>
        private readonly ConcurrentDictionary<IModel, BasicReturnEventArgs> _pendingReturns = new();
        private volatile bool _disposed;

        public RabbitMqPublishChannelPool(
            IRabbitMqPersistentConnection connection,
            string exchangeName,
            int maxSize,
            ILogger logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            _maxSize = maxSize <= 0 ? 1 : maxSize;
            _logger = logger;
            _slots = new SemaphoreSlim(_maxSize, _maxSize);
        }

        /// <summary>
        /// 从池中租用一条可用的 channel。调用方使用完后必须 <see cref="PooledChannel.Dispose"/> 归还。
        /// </summary>
        /// <param name="cancellationToken">等待槽位时尊重的取消令牌。</param>
        public PooledChannel Acquire(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RabbitMqPublishChannelPool));

            _slots.Wait(cancellationToken);
            try
            {
                while (_idle.TryDequeue(out var ch))
                {
                    if (ch.IsOpen)
                    {
                        // 清掉上一次租用残留的 return 事件,避免被本次误认作自己的失败
                        _pendingReturns.TryRemove(ch, out _);
                        return new PooledChannel(this, ch);
                    }
                    _pendingReturns.TryRemove(ch, out _);
                    TryDispose(ch);
                }
                var fresh = CreatePreparedChannel();
                _pendingReturns.TryRemove(fresh, out _);
                return new PooledChannel(this, fresh);
            }
            catch
            {
                _slots.Release();
                throw;
            }
        }

        /// <summary>
        /// 新建一条 channel 并完成"每 channel 仅做一次"的初始化:
        /// ExchangeDeclare + ConfirmSelect + BasicReturn 监听挂载。
        /// </summary>
        private IModel CreatePreparedChannel()
        {
            if (!_connection.IsConnected)
            {
                _connection.TryConnect();
            }

            var channel = _connection.CreateModel();
            try
            {
                channel.ExchangeDeclare(
                    exchange: _exchangeName,
                    type: "direct",
                    durable: true,
                    autoDelete: false,
                    arguments: null);
                // mandatory:true 时 broker 无路由会通过 BasicReturn 通知;
                // 必须挂监听,否则 confirms 也是 ack,业务以为成功但消息已丢
                channel.BasicReturn += OnChannelBasicReturn;
                // 开启 publisher confirms,后续每次 publish 后 WaitForConfirms 可同步等到 ack/return
                channel.ConfirmSelect();
                return channel;
            }
            catch
            {
                TryDispose(channel);
                throw;
            }
        }

        private void OnChannelBasicReturn(object sender, BasicReturnEventArgs ea)
        {
            // BasicProperties.MessageId 由 publisher 在发布前设置;这里能直接定位到原始消息
            _logger.LogWarning(
                "RabbitMQ publish 被退回(无可用路由) messageId={MessageId} exchange={Exchange} routingKey={RoutingKey} replyCode={ReplyCode} replyText={ReplyText}",
                ea.BasicProperties?.MessageId, ea.Exchange, ea.RoutingKey, ea.ReplyCode, ea.ReplyText);

            // 同步把 return 事件挂到 channel 上,供 WaitForConfirmsOrThrow 消费。
            // BasicReturn 在 channel IO 线程触发,publisher 仍在等 WaitForConfirms ——
            // 按 AMQP 协议 Return 帧在同一 publish 的 Ack 帧之前送达,所以 publisher 能在 WaitForConfirms 返回后看到这条记录
            if (sender is IModel ch)
            {
                _pendingReturns[ch] = ea;
            }
        }

        /// <inheritdoc />
        public BasicReturnEventArgs ConsumePendingReturn(IModel channel)
        {
            if (channel == null) return null;
            return _pendingReturns.TryRemove(channel, out var ea) ? ea : null;
        }

        /// <summary>
        /// 归还 channel。已关闭的 channel 不再回收,直接释放槽位让池规模缩回。
        /// </summary>
        /// <remarks>
        /// 接口契约要求公开,但调用方应总是通过 <see cref="PooledChannel.Dispose"/>(典型为 <c>using</c>)归还。
        /// </remarks>
        public void Return(IModel channel)
        {
            try
            {
                if (_disposed || channel == null || !channel.IsOpen)
                {
                    TryDispose(channel);
                    return;
                }
                _idle.Enqueue(channel);
            }
            finally
            {
                if (!_disposed)
                {
                    try { _slots.Release(); } catch (ObjectDisposedException) { /* race with Dispose */ }
                }
            }
        }

        private void TryDispose(IModel channel)
        {
            if (channel == null) return;
            try { channel.Close(); }
            catch (Exception ex) { _logger?.LogDebug(ex, "RabbitMQ channel close failed in pool"); }
            try { channel.Dispose(); }
            catch (Exception ex) { _logger?.LogDebug(ex, "RabbitMQ channel dispose failed in pool"); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            while (_idle.TryDequeue(out var ch))
            {
                TryDispose(ch);
            }
            _pendingReturns.Clear();
            try { _slots.Dispose(); } catch { /* 容忍 */ }
        }
    }

    /// <summary>
    /// 从 <see cref="IRabbitMqPublishChannelPool"/> 租出来的 channel 句柄,Dispose 时归还池中。
    /// </summary>
    public readonly struct PooledChannel : IDisposable
    {
        private readonly IRabbitMqPublishChannelPool _pool;
        public IModel Channel { get; }

        internal PooledChannel(IRabbitMqPublishChannelPool pool, IModel channel)
        {
            _pool = pool;
            Channel = channel;
        }

        /// <summary>
        /// 等待 broker 对本次 publish 的回执,并把"被退回 / nack / 超时"统一转成异常上抛。
        /// </summary>
        /// <param name="timeout">等待 ack/nack 的上限。</param>
        /// <exception cref="RabbitMqPublishReturnedException">mandatory:true 时 broker 退回(无可用路由)</exception>
        /// <exception cref="RabbitMqPublishUnconfirmedException">confirm 超时或 broker nack</exception>
        /// <remarks>
        /// 由 publisher 在 BasicPublish 之后立即调用;池外不应直接调 <see cref="IModel.WaitForConfirms()"/>,
        /// 否则会漏掉 BasicReturn 信号。
        /// </remarks>
        public void WaitForConfirmsOrThrow(TimeSpan timeout)
        {
            // WaitForConfirms 会阻塞到本 channel 所有未确认 publish 都得到 ack/nack 或超时。
            // 池里保证 channel 串行使用,所以未确认的有且只有刚才那一条
            var ackedAll = Channel.WaitForConfirms(timeout, out var timedOut);

            // Return 帧按 AMQP 协议先于 Ack 抵达,WaitForConfirms 返回时若有 return,已经在池里挂好
            var returned = _pool.ConsumePendingReturn(Channel);
            if (returned != null)
            {
                throw new RabbitMqPublishReturnedException(
                    returned.Exchange,
                    returned.RoutingKey,
                    returned.ReplyCode,
                    returned.ReplyText,
                    returned.BasicProperties?.MessageId);
            }

            if (timedOut)
            {
                throw new RabbitMqPublishUnconfirmedException(timedOut: true);
            }
            if (!ackedAll)
            {
                throw new RabbitMqPublishUnconfirmedException(timedOut: false);
            }
        }

        public void Dispose() => _pool.Return(Channel);
    }
}
