using Core.Ddd.Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Core.EntityFrameworkCore.Repositories
{
    /// <summary>
    /// 批量直接执行 SQL 的仓储能力接口,封装 EF Core 7+ 的 <c>ExecuteDelete</c> / <c>ExecuteUpdate</c> 操作。
    /// </summary>
    /// <typeparam name="TEntity">实体类型,需为引用类型并实现 <see cref="IEntity"/>。</typeparam>
    /// <remarks>
    /// <para><b>与主仓储 <see cref="Core.Ddd.Domain.Repositories.IRepository{TEntity}"/> 的语义差异</b>:</para>
    /// <list type="bullet">
    ///   <item><b>立即执行</b>:调用即向数据库发送 SQL,<u>不会等到 UoW 提交</u>。</item>
    ///   <item><b>绕过 ChangeTracker</b>:不加载实体到内存、不触发实体级钩子、不产生领域事件、不更新审计字段。</item>
    ///   <item><b>事务边界靠调用方控制</b>:本接口的方法在无外层显式事务时各自独立,不会和同一 UoW 内的其他
    ///   <c>AddAsync</c>/<c>RemoveAsync</c> 合并提交。如需原子性,请用 <c>TransactionScope</c> 或显式开
    ///   <c>IDbContextTransaction</c> 包住调用。</item>
    /// </list>
    /// <para><b>适用场景</b>:
    /// <list type="bullet">
    ///   <item>大表清理(几十万行 DELETE),避免 ChangeTracker 路径带来的 N 次 SELECT 与内存膨胀。</item>
    ///   <item>状态翻转/字段刷值(把符合某条件的所有记录 IsActive 置 false 之类),不关心审计字段/领域事件。</item>
    /// </list>
    /// </para>
    /// <para><b>不适用</b>:涉及业务事件、审计字段更新、聚合一致性校验的更新——这些应该走 <c>IRepository</c> + UoW。</para>
    /// </remarks>
    public interface IBulkRepository<TEntity> where TEntity : class, IEntity
    {
        /// <summary>
        /// 直接发送 <c>DELETE</c> 语句删除所有满足条件的实体,返回受影响行数。
        /// </summary>
        /// <param name="predicate">过滤表达式,语义同 LINQ <c>Where</c>;<c>null</c> 视为无条件全表删除,请谨慎使用。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>实际被删除的行数。</returns>
        /// <remarks>对应 EF Core 的 <c>IQueryable.ExecuteDeleteAsync()</c>。</remarks>
        Task<int> ExecuteDeleteAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 直接发送 <c>UPDATE</c> 语句批量更新所有满足条件的实体,返回受影响行数。
        /// </summary>
        /// <param name="predicate">过滤表达式。</param>
        /// <param name="setPropertyCalls">
        /// 字段赋值委托,使用 <see cref="UpdateSettersBuilder{T}"/> 的 <c>SetProperty</c> 调用进行配置。
        /// 例如:<c>s =&gt; { s.SetProperty(x =&gt; x.Status, 1); s.SetProperty(x =&gt; x.UpdateTime, now); }</c>。
        /// </param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>实际被更新的行数。</returns>
        /// <remarks>对应 EF Core 10 的 <c>IQueryable.ExecuteUpdateAsync()</c>。注意 EF Core 10 起签名从
        /// <c>Expression&lt;Func&lt;SetPropertyCalls&lt;T&gt;, SetPropertyCalls&lt;T&gt;&gt;&gt;</c>
        /// 改为 <c>Action&lt;UpdateSettersBuilder&lt;T&gt;&gt;</c>。</remarks>
        Task<int> ExecuteUpdateAsync(
            Expression<Func<TEntity, bool>> predicate,
            Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
            CancellationToken cancellationToken = default);
    }
}
