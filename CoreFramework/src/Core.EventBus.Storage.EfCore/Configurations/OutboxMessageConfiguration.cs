using Core.EventBus.Storage.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.EventBus.Storage.EfCore.Configurations
{
    /// <summary>
    /// <see cref="OutboxMessageEntity"/> 的 EF 配置。
    /// 由 <c>modelBuilder.AddEventBusStorage()</c> 调用 <c>ApplyConfiguration</c> 注入。
    /// </summary>
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
    {
        /// <summary>表名常量；如需自定义请 fork 一份替换。</summary>
        public const string TableName = "EventBus_Outbox";

        public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
        {
            builder.ToTable(TableName);
            builder.HasKey(x => x.Id);

            // 长度约束的来源：
            //   AssemblyName / MessageName 长度由 CLR 类型决定，常见值远小于 256/512
            //   MessageData 不限长（业务事件 payload）
            //   LastError 截断至 4000 防止单行异常堆栈撑爆 schema
            builder.Property(x => x.Version).IsRequired();
            builder.Property(x => x.AssemblyName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.MessageName).IsRequired().HasMaxLength(512);
            builder.Property(x => x.MessageData).IsRequired();
            builder.Property(x => x.CreateTime).IsRequired();
            builder.Property(x => x.UtcTime).IsRequired();
            builder.Property(x => x.RetryCount).IsRequired();
            builder.Property(x => x.NextRetryAt);
            builder.Property(x => x.LastError).HasMaxLength(4000);

            // Dispatcher 的 FetchReadyAsync 形如：
            //   WHERE NextRetryAt IS NULL OR NextRetryAt <= NOW()  ORDER BY UtcTime
            // 该复合索引覆盖了 WHERE 与 ORDER BY 两个维度，避免大表全表扫描
            builder.HasIndex(x => new { x.NextRetryAt, x.UtcTime })
                .HasDatabaseName("IX_EventBus_Outbox_Ready");
        }
    }
}
