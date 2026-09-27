using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class PropertyReminderConfiguration : AuditableEntityConfiguration<PropertyReminder>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PropertyReminder> builder)
    {
        builder.ToTable("PropertyReminders");
        builder.Ignore(reminder => reminder.IsOverdue);

        builder.Property(reminder => reminder.Title).HasMaxLength(200).IsRequired();
        builder.Property(reminder => reminder.Description).HasColumnType("text");
        builder.Property(reminder => reminder.DueDate).IsRequired();
        builder.Property(reminder => reminder.Priority).HasStringEnumConversion(20);
        builder.Property(reminder => reminder.Status).HasStringEnumConversion(30);

        builder.HasIndex(reminder => reminder.DueDate)
            .HasDatabaseName("IX_PropertyReminders_DueDate");
    }
}
