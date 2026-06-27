using Core.EventBus.Storage.EfCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Core.EventBus.Storage.EfCore
{
    /// <summary><see cref="ModelBuilder"/> 扩展:把 outbox / inbox / 死信三张表的 EF 配置加入业务 DbContext。</summary>
    /// <remarks>
    /// outbox 表必须与业务表共享同一连接 / 事务才能保证"业务 + 事件"原子落库;
    /// 放进业务 DbContext 让 SaveChanges 自然把两类变更一起提交。
    /// </remarks>
    public static class EventBusModelBuilderExtensions
    {
        /// <summary>把 outbox / inbox / 死信表的 EF 配置应用到当前 DbContext;在 OnModelCreating 中调用。</summary>
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
