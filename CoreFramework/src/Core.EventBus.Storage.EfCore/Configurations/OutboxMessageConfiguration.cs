using Core.EventBus.Storage.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.EventBus.Storage.EfCore.Configurations
{
    /// <summary><see cref="OutboxMessageEntity"/> 的 EF 配置;由 <c>modelBuilder.AddEventBusStorage()</c> 注入。</summary>
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
    {
        /// <summary>表名常量;如需自定义请 fork 替换。</summary>
        public const string TableName = "EventBus_Outbox";

        public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
        {
            builder.ToTable(TableName);
            builder.HasKey(x => x.Id);

            // AssemblyName / MessageName 常见值远小于 256/512;MessageData 不限长;
            // LastError 截断 4000 防止单行异常堆栈撑爆 schema
            builder.Property(x => x.MessageId).IsRequired();
            builder.Property(x => x.Version).IsRequired();
            builder.Property(x => x.AssemblyName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.MessageName).IsRequired().HasMaxLength(512);
            builder.Property(x => x.MessageData).IsRequired();
            builder.Property(x => x.CreateTime).IsRequired();
            builder.Property(x => x.UtcTime).IsRequired();
            builder.Property(x => x.RetryCount).IsRequired();
            builder.Property(x => x.NextRetryAt);
            builder.Property(x => x.LastError).HasMaxLength(4000);

            // FetchReadyAsync 形如 WHERE NextRetryAt IS NULL OR NextRetryAt <= NOW() ORDER BY UtcTime;
            // 该复合索引覆盖 WHERE + ORDER BY,避免大表全表扫描
            builder.HasIndex(x => new { x.NextRetryAt, x.UtcTime })
                .HasDatabaseName("IX_EventBus_Outbox_Ready");

            // 运维侧按业务 MessageId 反查投递状态(还在 outbox / 已删 / 进死信);
            // 不加唯一约束,允许同一 MessageId 被业务有意重发(应换 Id,这里给灵活度)
            builder.HasIndex(x => x.MessageId)
                .HasDatabaseName("IX_EventBus_Outbox_MessageId");
        }
    }
}
