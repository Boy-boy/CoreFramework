using Core.EventBus;
using Core.EventBus.Diagnostics;
using Core.EventBus.Inbox;
using Core.EventBus.Storage.EfCore.Entities;
using Core.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary><see cref="IMessageHandlerInvoker"/> 的事务一致性 + 幂等实现,启用存储时替换默认实现。</summary>
    /// <remarks>
    /// <para>每个 handler 调用流程:</para>
    /// <list type="number">
    ///   <item><description>新建 DI scope:DbContext / UoW / handler 同生命周期</description></item>
    ///   <item><description>开 transactional UoW</description></item>
    ///   <item><description><see cref="IInboxStorage.TryAcquireAsync"/> 登记 inbox 行(不 SaveChanges);返回 false 即 commit 空事务后 return(让 broker 能 ack)</description></item>
    ///   <item><description>反射调 <c>HandleAsync</c></description></item>
    ///   <item><description>commit:业务行 + inbox 行原子落库</description></item>
    ///   <item><description>异常 rollback + rethrow → 上游不 ack → broker 重投</description></item>
    /// </list>
    /// <para>关键不变式:</para>
    /// <list type="bullet">
    ///   <item><description>inbox 行不能早于 handler 业务 commit,否则失败后下次重投会误判为已处理</description></item>
    ///   <item><description>每次调用独立 scope,否则上次 DbContext 被复用产生脏数据</description></item>
    ///   <item><description>rollback 用 <see cref="CancellationToken.None"/>:即使上游 ct 已 cancel 也必须跑完,避免事务/连接泄漏</description></item>
    /// </list>
    /// <para>consumerGroup 默认为 <c>handlerType.FullName</c>;多 handler 订阅同一消息时各自有独立去重记录。</para>
    /// </remarks>
    public sealed class InboxAwareMessageHandlerInvoker : IMessageHandlerInvoker
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InboxAwareMessageHandlerInvoker> _logger;

        public InboxAwareMessageHandlerInvoker(
            IServiceScopeFactory scopeFactory,
            ILogger<InboxAwareMessageHandlerInvoker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task InvokeAsync(
            Type messageType,
            Type handlerType,
            IMessage message,
            CancellationToken cancellationToken = default)
        {
            // 新 scope:DbContext / UoW / handler 与本次消费一一对应
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sp = scope.ServiceProvider;

            var uowMgr = sp.GetRequiredService<IUnitOfWorkManager>();

            // inbox 可选:未注册时退化为"只有 UoW 包装"
            var inbox = sp.GetService<IInboxStorage>();

            // 用异步 Begin,避免同步 Begin 在消费热路径上阻塞线程池
            await using var uow = await uowMgr.BeginAsync(
                new UnitOfWorkOptions(isTransactional: true),
                cancellationToken);

            try
            {
                if (inbox != null)
                {
                    // 优先用 [InboxConsumerGroup] 显式声明的标识,避免 handler 重命名导致 inbox 失效
                    var consumerGroup = InboxConsumerGroupAttribute.GetGroupOrDefault(handlerType);
                    var acquired = await inbox.TryAcquireAsync(message.Id, consumerGroup, cancellationToken);
                    if (!acquired)
                    {
                        _logger.LogDebug(
                            "跳过重复消费 messageId={MessageId} handler={Handler}",
                            message.Id, consumerGroup);
                        EventBusMetrics.InboxDuplicate.Add(1,
                            new KeyValuePair<string, object>("messageName", MessageNameAttribute.GetNameOrDefault(messageType)),
                            new KeyValuePair<string, object>("handlerType", handlerType.FullName ?? handlerType.Name));
                        // 仍要 commit,否则上游不 ack → 不停重投
                        await uow.CommitAsync(cancellationToken);
                        return;
                    }
                }

                // 本 scope 内 resolve handler,保证它注入的 DbContext / Repository 与本 UoW 是同一份
                var handler = sp.GetRequiredService(handlerType);
                var method = DefaultMessageHandlerInvoker.ResolveHandleMethod(messageType);
                try
                {
                    await ((Task)method.Invoke(handler, [message, cancellationToken])!);
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    // 按原异常 + 原栈重新抛出,让下面 catch 走 rollback
                    ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                }

                // 业务行 + inbox 行原子落库
                await uow.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException dbEx) when (IsInboxDuplicateKey(dbEx))
            {
                // 并发:另一消费者先一步登记 inbox 行,本次 commit 主键冲突。语义上等价于"已处理过",
                // 回滚业务变更后不再抛 —— 上游可以正常 ack 跳过
                _logger.LogDebug(
                    "并发消费已先一步登记 inbox messageId={MessageId} handler={Handler},本次跳过",
                    message.Id, handlerType.FullName);
                try { await uow.RollbackAsync(CancellationToken.None); }
                catch (Exception rbEx)
                {
                    _logger.LogError(rbEx, "Rollback 失败 messageId={MessageId}", message.Id);
                }
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Handler {Handler} 处理 {MessageId} 失败，回滚事务",
                    handlerType.FullName, message.Id);

                EventBusMetrics.HandlerFailures.Add(1,
                    new KeyValuePair<string, object>("messageName", MessageNameAttribute.GetNameOrDefault(messageType)),
                    new KeyValuePair<string, object>("handlerType", handlerType.FullName ?? handlerType.Name));

                // rollback 用 CancellationToken.None:即使上游 ct 已 cancel 也必须跑完
                try { await uow.RollbackAsync(CancellationToken.None); }
                catch (Exception rbEx)
                {
                    _logger.LogError(rbEx, "Rollback 失败 messageId={MessageId}", message.Id);
                }

                // 重抛:上游通常不 ack → broker 重投
                throw;
            }
        }

        /// <summary>判定 <see cref="DbUpdateException"/> 是否由 inbox 行冲突触发(并发"先到者已登记"信号)。</summary>
        /// <remarks>
        /// <para>用 <see cref="DbUpdateException.Entries"/> 而非错误消息字符串识别:
        /// 不依赖 provider 错误码/文案,业务表 unique 冲突也不会误判。</para>
        /// <para>判定标准:失败的 entry 集合中存在 <see cref="InboxMessageEntity"/> 且处于 <see cref="EntityState.Added"/>。
        /// EF Core 在 SaveChanges 失败时会把抛错那一行(及相关行)挂到 <c>Entries</c> 上。</para>
        /// </remarks>
        private static bool IsInboxDuplicateKey(DbUpdateException ex)
        {
            return ex.Entries.Any(e =>
                e.Entity is InboxMessageEntity && e.State == EntityState.Added);
        }
    }
}
