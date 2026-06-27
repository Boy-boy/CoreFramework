using Core.EventBus.Storage.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.EventBus.Storage.EfCore.Configurations
{
    /// <summary><see cref="InboxMessageEntity"/> 的 EF 配置。</summary>
    /// <remarks>
    /// <c>(MessageId, ConsumerGroup)</c> 复合主键直接承担"同一消息 × 同一 handler 只允许一次"的去重约束;
    /// 第二次 INSERT 主键冲突,<see cref="EfCoreInboxStorage{TDbContext}"/> 据此返回 false。
    /// <para><c>InboxCleanupService</c> 执行 <c>WHERE ProcessedAtUtc &lt; threshold</c>,单列索引足够。</para>
    /// </remarks>
    public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessageEntity>
    {
        public const string TableName = "EventBus_Inbox";

        public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
        {
            builder.ToTable(TableName);
            builder.HasKey(x => new { x.MessageId, x.ConsumerGroup });
            builder.Property(x => x.ConsumerGroup).IsRequired().HasMaxLength(256);
            builder.Property(x => x.ProcessedAtUtc).IsRequired();

            builder.HasIndex(x => x.ProcessedAtUtc)
                .HasDatabaseName("IX_EventBus_Inbox_ProcessedAt");
        }
    }
}
