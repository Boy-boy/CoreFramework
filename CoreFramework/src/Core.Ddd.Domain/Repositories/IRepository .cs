using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Core.Ddd.Domain.Entities;

namespace Core.Ddd.Domain.Repositories
{
    /// <summary>
    /// 仓储标记接口。本身不含任何成员,仅用于在依赖注入容器中统一识别和扫描仓储类型。
    /// </summary>
    public interface IRepository
    {
    }

    /// <summary>
    /// 聚合根(或实体)的通用仓储接口,封装对 <typeparamref name="TEntity"/> 的查询与持久化操作。
    /// </summary>
    /// <typeparam name="TEntity">仓储管理的实体类型,需为引用类型并实现 <see cref="IEntity"/>。</typeparam>
    /// <remarks>
    /// 设计约定:
    /// <list type="bullet">
    ///   <item>所有写入类方法(<c>AddAsync</c> / <c>RemoveAsync</c> / <c>ReloadAsync</c> / <c>UpdateAsync</c>)
    ///   仅修改 EF Core 的 ChangeTracker 状态,不会立即落库。</item>
    ///   <item>真正的持久化由外层工作单元(UoW)在 <c>CommitAsync</c> 时统一提交,
    ///   以保证同一业务流的多个仓储操作具有事务原子性。</item>
    ///   <item>仓储内部不允许直接调用 <c>SaveChanges</c> / <c>SaveChangesAsync</c>。</item>
    /// </list>
    /// </remarks>
    public interface IRepository<TEntity> : IRepository
        where TEntity : class, IEntity
    {
        /// <summary>
        /// 获取实体的可组合查询源。调用方可在其上继续叠加 <c>Where</c> / <c>OrderBy</c> / <c>Include</c> 等表达式,
        /// 直到调用 <c>ToListAsync</c> / <c>FirstOrDefaultAsync</c> 等异步终结操作时才会真正执行 SQL。
        /// </summary>
        /// <returns>当前实体集合对应的 <see cref="IQueryable{T}"/>。</returns>
        Task<IQueryable<TEntity>> GetQueryableAsync();

        /// <summary>
        /// 将单个实体加入 ChangeTracker 并标记为新增状态。
        /// </summary>
        /// <param name="entity">要新增的实体。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <remarks>
        /// 不会立即写库,需由 UoW 提交。底层使用 EF Core 的 <c>DbSet.AddAsync</c>,
        /// 因此对于需要异步生成主键的场景(如 SQL Server Hi/Lo 序列)也能正确处理。
        /// </remarks>
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量将实体加入 ChangeTracker。
        /// </summary>
        /// <param name="entities">要新增的实体集合,不可为 <c>null</c>。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按条件统计满足表达式的实体数量。
        /// </summary>
        /// <param name="expression">过滤表达式,不可为 <c>null</c>。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>满足条件的实体总数,使用 <see cref="long"/> 以支持大表场景。</returns>
        Task<long> CountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

        /// <summary>
        /// 统计实体集合的全部数量(不带过滤)。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>实体总数。</returns>
        Task<long> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 判断是否存在满足条件的实体,语义对应 LINQ 的 <c>Any</c>。
        /// </summary>
        /// <param name="expression">过滤表达式。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>存在返回 <c>true</c>,否则 <c>false</c>。SQL 层面使用 <c>EXISTS</c>,无需完整扫描。</returns>
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

        /// <summary>
        /// 判断当前表是否存在任意实体(不带条件)。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表中至少有一行返回 <c>true</c>,空表返回 <c>false</c>。常用于初始化前的存在性检查。</returns>
        Task<bool> AnyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询满足条件的首个实体,无匹配时返回 <c>null</c>。语义对应 LINQ 的 <c>FirstOrDefault</c>。
        /// </summary>
        /// <param name="expression">过滤表达式。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>命中的首个实体;无匹配时为 <c>null</c>。</returns>
        Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

        /// <summary>
        /// 不带条件返回任意一条实体(语义为"取一条样本"),空表时返回 <c>null</c>。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>任意一条实体或 <c>null</c>。结果顺序不保证,如需稳定排序请改用 <see cref="GetQueryableAsync"/> 自行构造。</returns>
        Task<TEntity> FindAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 按条件查询恰好一条实体,语义对应 LINQ 的 <c>SingleOrDefault</c>。
        /// </summary>
        /// <param name="expression">过滤表达式,通常应基于业务唯一键。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>命中的唯一实体;无匹配时返回 <c>null</c>。</returns>
        /// <exception cref="InvalidOperationException">命中多于一条实体时抛出。用于在唯一性假设被打破时立即报错,
        /// 而不是像 <see cref="FirstOrDefaultAsync(Expression{Func{TEntity, bool}}, CancellationToken)"/> 那样静默返回首条。</exception>
        Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询所有满足条件的实体并以列表返回。
        /// </summary>
        /// <param name="expression">过滤表达式。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>命中的实体列表;无匹配时返回空列表(非 <c>null</c>)。</returns>
        /// <remarks>注意:本方法会一次性把全部结果加载到内存,大结果集请改用 <see cref="GetPagedListAsync(int, int, Expression{Func{TEntity, bool}}, CancellationToken)"/>。</remarks>
        Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将单个实体标记为删除。实际 DELETE 语句由 UoW 提交时执行。
        /// </summary>
        /// <param name="entity">要删除的实体,应为当前上下文已跟踪的实体。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量标记实体为删除。
        /// </summary>
        /// <param name="entities">要删除的实体集合。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task RemoveAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// 从数据库重新加载实体,覆盖跟踪上下文中已有的属性值。
        /// </summary>
        /// <param name="entity">需要重新加载的已跟踪实体。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <remarks>
        /// 典型用途:丢弃本地未提交的修改、获取最新的行版本/RowVersion、长事务中刷新缓存的实体快照。
        /// </remarks>
        Task ReloadAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将实体标记为 <c>Modified</c>,使 UoW 提交时生成 UPDATE 语句。
        /// </summary>
        /// <param name="entity">要更新的实体。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <remarks>
        /// 主要用于游离(detached)实体回写场景。
        /// 对于已被当前上下文跟踪的实体,属性变更后 EF Core 会通过 ChangeTracker 自动检测,无需显式调用此方法。
        /// </remarks>
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量将实体标记为 <c>Modified</c>。
        /// </summary>
        /// <param name="entities">要更新的实体集合。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <remarks>与 <see cref="AddAsync(IEnumerable{TEntity}, CancellationToken)"/> / <see cref="RemoveAsync(IEnumerable{TEntity}, CancellationToken)"/> 对齐。</remarks>
        Task UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按条件分页查询实体。
        /// </summary>
        /// <param name="pageIndex">页码,<b>从 1 开始</b>。小于 1 抛 <see cref="ArgumentException"/>。</param>
        /// <param name="pageSize">每页条数,必须大于 0;否则抛 <see cref="ArgumentException"/>。</param>
        /// <param name="expression">过滤表达式,可为 <c>null</c>(等同于无过滤)。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>包含当前页数据(<c>DataEnumerable</c>)和命中总数(<c>Total</c>)的元组。</returns>
        /// <exception cref="ArgumentException"><paramref name="pageIndex"/> &lt; 1 或 <paramref name="pageSize"/> &lt;= 0。</exception>
        Task<(IEnumerable<TEntity> DataEnumerable, long Total)> GetPagedListAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>> expression,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 基于已构造好的 <see cref="IQueryable{T}"/> 分页查询。
        /// 适合复杂查询、需要 <c>Include</c> 预加载、跨表 join 等无法用单一 lambda 表达的场景。
        /// </summary>
        /// <param name="pageIndex">页码,<b>从 1 开始</b>。</param>
        /// <param name="pageSize">每页条数,必须大于 0。</param>
        /// <param name="queryable">已构造的查询源,通常由 <see cref="GetQueryableAsync"/> 起步并叠加条件得到。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>包含当前页数据和命中总数的元组。</returns>
        /// <exception cref="ArgumentException"><paramref name="pageIndex"/> &lt; 1 或 <paramref name="pageSize"/> &lt;= 0。</exception>
        Task<(IEnumerable<TEntity> DataEnumerable, long Total)> GetPagedListAsync(
            int pageIndex,
            int pageSize,
            IQueryable<TEntity> queryable,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 带主键的通用仓储接口,在 <see cref="IRepository{TEntity}"/> 基础上提供按主键的查询与删除。
    /// </summary>
    /// <typeparam name="TEntity">实体类型,需实现 <see cref="IEntity{TKey}"/>。</typeparam>
    /// <typeparam name="TKey">主键类型(逆变),通常为 <see cref="int"/> / <see cref="long"/> / <see cref="Guid"/> / <see cref="string"/>。</typeparam>
    public interface IRepository<TEntity, in TKey> : IRepository<TEntity>
        where TEntity : class, IEntity<TKey>
    {
        /// <summary>
        /// 按主键标记实体为删除。
        /// </summary>
        /// <param name="key">实体主键。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <remarks>
        /// 内部先按主键查出实体,若未命中则静默返回不做任何操作(不抛异常)。
        /// 注意这会触发一次 SELECT,如果调用方已经持有实体引用,优先使用 <see cref="IRepository{TEntity}.RemoveAsync(TEntity, CancellationToken)"/>。
        /// </remarks>
        Task RemoveAsync(TKey key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按主键查询实体,未命中返回 <c>null</c>。
        /// </summary>
        /// <param name="key">实体主键。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>命中的实体或 <c>null</c>。</returns>
        Task<TEntity> FindAsync(TKey key, CancellationToken cancellationToken = default);
    }
}
