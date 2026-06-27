using Core.EventBus;
using Core.EventBus.Inbox;
using Core.EventBus.Storage.EfCore.Configurations;
using Core.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>
    /// <see cref="IMessageHandlerInvoker"/> 的"事务一致性 + 幂等"实现。
    /// 当用户注册 <c>AddEfCoreEventBusStorage&lt;TDbContext&gt;()</c> 时，本实现会替换默认实现。
    /// </summary>
    /// <remarks>
    /// <para><b>调用每个 handler 的完整流程</b></para>
    /// <list type="number">
    ///   <item><description>新建 DI scope：让本次消费的 DbContext / UoW / handler 共享同一生命周期</description></item>
    ///   <item><description>开 transactional UoW：handler 业务变更 + inbox 行都将随之一起 commit / rollback</description></item>
    ///   <item><description>调 <see cref="IInboxStorage.TryAcquireAsync"/> 登记 inbox 行（不 SaveChanges，等 commit）
    ///     <list type="bullet">
    ///       <item><description>返回 false（已处理）→ commit 一个空事务（让 broker 能 ack）→ return</description></item>
    ///     </list>
    ///   </description></item>
    ///   <item><description>resolve handler，反射调用 <c>HandleAsync(message)</c></description></item>
    ///   <item><description>commit UoW：业务行 + inbox 行原子落库</description></item>
    ///   <item><description>异常路径：rollback（含 inbox 行）+ rethrow → 上游 subscriber 不 ack → broker 重投</description></item>
    /// </list>
    ///
    /// <para><b>关键不变式</b></para>
    /// <list type="bullet">
    ///   <item><description>inbox 行不能"先于" handler 业务 commit，否则一旦 handler 失败，
    ///   inbox 行先落了 → 下次重投以为已处理 → 业务永远丢；</description></item>
    ///   <item><description>scope 必须在每次调用一一对应，否则上次的 DbContext 会被本次复用 → 脏数据；</description></item>
    ///   <item><description>异常 rollback 用 <see cref="CancellationToken.None"/>：即使上游 ct 已 cancel，
    ///   也要保证 rollback 能跑完，避免事务/连接泄漏。</description></item>
    /// </list>
    ///
    /// <para><b>ConsumerGroup 取值</b></para>
    /// <para>
    /// 默认使用 <c>handlerType.FullName</c>。同一消息被多个 handler 订阅时，每个 handler 在 inbox 表
    /// 有独立的去重记录 —— 单个 handler 失败重投不会影响其他 handler 的处理进度。
    /// </para>
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
            // ① 新建 scope：让 DbContext / UoW / handler 与本次消费一一对应
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sp = scope.ServiceProvider;

            var uowMgr = sp.GetRequiredService<IUnitOfWorkManager>();

            // inbox 可选：用户没注册时 sp.GetService 返回 null，本调用退化为"只有 UoW 包装"
            var inbox = sp.GetService<IInboxStorage>();

            // ② 开 transactional UoW（与 OutboxDispatcher 用法一致,避免同步 Begin 在消费热路径上阻塞线程池）
            await using var uow = await uowMgr.BeginAsync(
                new UnitOfWorkOptions(isTransactional: true),
                cancellationToken);

            try
            {
                // ③ inbox 去重
                if (inbox != null)
                {
                    var consumerGroup = handlerType.FullName ?? handlerType.Name;
                    var acquired = await inbox.TryAcquireAsync(message.Id, consumerGroup, cancellationToken);
                    if (!acquired)
                    {
                        _logger.LogDebug(
                            "跳过重复消费 messageId={MessageId} handler={Handler}",
                            message.Id, consumerGroup);
                        // 注意：仍要 commit。否则会出现"UoW 没 commit → broker 也没 ack → 不停重投"
                        await uow.CommitAsync(cancellationToken);
                        return;
                    }
                }

                // ④ 在本 scope 内 resolve handler，确保它注入的 DbContext / Repository 与本 UoW 是同一份
                var handler = sp.GetRequiredService(handlerType);
                var method = DefaultMessageHandlerInvoker.ResolveHandleMethod(messageType);
                try
                {
                    await ((Task)method.Invoke(handler, [message, cancellationToken])!);
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    // 反射调用抛出的异常被包了一层；按原异常 + 原栈重新抛出，
                    // 让下面 catch (Exception) 拿到真正的业务异常去走 rollback / 上抛
                    ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                }

                // ⑤ commit：业务行 + inbox 行原子落库
                await uow.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException dbEx) when (IsInboxDuplicateKey(dbEx))
            {
                // 并发场景:另一并发消费已经先一步成功登记 inbox 行,本次 commit 因主键冲突失败。
                // 语义上等价于"已处理过",回滚业务变更后不再抛 —— 上游 subscriber 可以正常 ack 跳过。
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

                // rollback 必须用 CancellationToken.None：
                //   假如上游 cancellationToken 已 cancel（例如关进程），
                //   rollback 自身不能被中断，否则会留下未关闭的事务和连接
                try { await uow.RollbackAsync(CancellationToken.None); }
                catch (Exception rbEx)
                {
                    _logger.LogError(rbEx, "Rollback 失败 messageId={MessageId}", message.Id);
                }

                // 重抛 → 上游 subscriber 决定是否 ack；典型实现是不 ack → broker 重投
                throw;
            }
        }

        /// <summary>
        /// 判定一个 <see cref="DbUpdateException"/> 是否由 inbox 表主键冲突触发。
        /// 这是常并发场景下"另一消费者已先一步登记"的等价信号。
        /// </summary>
        /// <remarks>
        /// 不同 provider 的内层异常类型不同(SqlException / PostgresException / MySqlException...),
        /// 这里采用最稳健的多 provider 兼容方式:检查内层异常文本里是否同时包含
        /// "duplicate"/"PRIMARY"/"23505" 等典型主键冲突标识 +inbox 表名。
        /// </remarks>
        private static bool IsInboxDuplicateKey(DbUpdateException ex)
        {
            var inner = ex.InnerException;
            if (inner == null) return false;
            var msg = inner.Message ?? string.Empty;
            var tableName = InboxMessageConfiguration.TableName;
            var hasInboxRef = msg.IndexOf(tableName, StringComparison.OrdinalIgnoreCase) >= 0;
            var hasDuplicateSignal =
                msg.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("PRIMARY", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("UNIQUE", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("23505", StringComparison.Ordinal) >= 0
                || msg.IndexOf("2627", StringComparison.Ordinal) >= 0   // SqlServer
                || msg.IndexOf("2601", StringComparison.Ordinal) >= 0;  // SqlServer
            return hasInboxRef && hasDuplicateSignal;
        }
    }
}
