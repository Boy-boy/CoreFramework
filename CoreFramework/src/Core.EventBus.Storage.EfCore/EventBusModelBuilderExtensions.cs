using Core.EventBus.Storage.EfCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary>
    /// <see cref="ModelBuilder"/> 扩展，把 outbox / inbox / 死信三张表的 EF 配置一次性加入业务 DbContext。
    /// </summary>
    /// <remarks>
    /// <para><b>典型用法</b></para>
    /// <code>
    /// public class CustomerDbContext : CoreDbContext
    /// {
    ///     protected override void OnModelCreating(ModelBuilder modelBuilder)
    ///     {
    ///         base.OnModelCreating(modelBuilder);
    ///         modelBuilder.AddEventBusStorage();   // outbox / inbox / 死信
    ///     }
    /// }
    /// </code>
    /// <para><b>为什么放在业务 DbContext</b></para>
    /// <para>
    /// outbox 表必须与业务表共享同一个数据库连接 / 同一个事务，才能保证"业务 + 事件"原子落库。
    /// 把它放进业务 DbContext 是最简单的实现方式 —— SaveChanges 自然把两类变更一起提交。
    /// </para>
    /// </remarks>
    public static class EventBusModelBuilderExtensions
    {
        /// <summary>
        /// 把 outbox / inbox / 死信表的 EF 配置应用到当前 DbContext。
        /// 必须在 <see cref="DbContext.OnModelCreating"/> 中调用。
        /// </summary>
        public static ModelBuilder AddEventBusStorage(this ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new System.ArgumentNullException(nameof(modelBuilder));
            modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
            modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration());
            modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
            return modelBuilder;
        }
    }
}
