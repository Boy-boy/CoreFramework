using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;

namespace Core.RabbitMQ
{
    /// <summary>
    /// RabbitMQ publish channel 池契约。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 抽出接口的目的:让 publisher / outbox sender 等调用方依赖契约而非具体实现,
    /// 也便于单测里替换为 in-memory fake。默认实现见 <see cref="RabbitMqPublishChannelPool"/>。
    /// </para>
    /// <para>
    /// 使用模式:
    /// <code>
    /// using var rental = pool.Acquire(ct);
    /// rental.Channel.BasicPublish(...);
    /// rental.Channel.WaitForConfirms(...);
    /// // rental.Dispose() 自动归还,无须手动调 Return
    /// </code>
    /// </para>
    /// </remarks>
    public interface IRabbitMqPublishChannelPool : IDisposable
    {
        /// <summary>
        /// 租用一条可用 channel,以 <see cref="PooledChannel"/> 形式返回。
        /// 调用方使用 <c>using</c> 模式即可在退出 scope 时归还。
        /// </summary>
        /// <param name="cancellationToken">等待槽位时尊重的取消令牌。</param>
        PooledChannel Acquire(CancellationToken cancellationToken = default);

        /// <summary>
        /// 归还 channel 到池。
        /// </summary>
        /// <remarks>
        /// <b>不要直接调用本方法</b> —— 总是通过 <see cref="PooledChannel.Dispose"/>(典型为 <c>using</c>)归还,
        /// 避免漏归还导致槽位永久占用。
        /// 之所以暴露在接口上,是因为 <see cref="PooledChannel"/> 需要通过本契约触达任意实现的归还逻辑。
        /// </remarks>
        void Return(IModel channel);

        /// <summary>
        /// 取出当前 channel 自上次 Acquire 以来积累的 BasicReturn 事件并清空。
        /// </summary>
        /// <returns>有 return → 返回 args;无 return → 返回 null。</returns>
        /// <remarks>
        /// <see cref="PooledChannel.WaitForConfirmsOrThrow"/> 在 WaitForConfirms 之后调用,
        /// 用于把 mandatory:true 路径上"无路由 → 静默退回"转成显式 <see cref="RabbitMqPublishReturnedException"/>。
        /// </remarks>
        BasicReturnEventArgs ConsumePendingReturn(IModel channel);
    }
}
