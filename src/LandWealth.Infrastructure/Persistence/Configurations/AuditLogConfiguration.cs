using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).ValueGeneratedOnAdd();

        builder.Property(log => log.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(log => log.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(log => log.Action).HasStringEnumConversion(50);
        builder.Property(log => log.Timestamp).IsRequired();
        builder.Property(log => log.OldValues).HasColumnType("json");
        builder.Property(log => log.NewValues).HasColumnType("json");
        builder.Property(log => log.IpAddress).HasMaxLength(45);
        builder.Property(log => log.UserAgent).HasMaxLength(255);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(log => log.UserId).HasDatabaseName("IX_AuditLogs_UserId");
        builder.HasIndex(log => log.Timestamp).HasDatabaseName("IX_AuditLogs_Timestamp");
    }
}
