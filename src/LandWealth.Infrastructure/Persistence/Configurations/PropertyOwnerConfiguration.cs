using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class PropertyOwnerConfiguration : AuditableEntityConfiguration<PropertyOwner>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PropertyOwner> builder)
    {
        builder.ToTable("PropertyOwners");
        builder.Ignore(owner => owner.IsActive);

        builder.Property(owner => owner.OwnerName).HasMaxLength(150).IsRequired();
        builder.Property(owner => owner.OwnershipPercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(owner => owner.OwnershipType).HasStringEnumConversion(50);
        builder.Property(owner => owner.StartDate).IsRequired();
        builder.Property(owner => owner.Notes).HasMaxLength(500);

        builder.HasIndex(owner => owner.PropertyId)
            .HasDatabaseName("IX_PropertyOwners_PropertyId");
    }
}
