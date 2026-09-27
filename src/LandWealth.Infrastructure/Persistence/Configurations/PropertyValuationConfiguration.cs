using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class PropertyValuationConfiguration : AuditableEntityConfiguration<PropertyValuation>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PropertyValuation> builder)
    {
        builder.ToTable("PropertyValuations");

        builder.Property(valuation => valuation.ValuationDate).IsRequired();
        builder.Property(valuation => valuation.EstimatedValue).IsRequired();
        builder.Property(valuation => valuation.ValuationSource).HasStringEnumConversion(100);
        builder.Property(valuation => valuation.Notes).HasColumnType("text");

        builder.HasIndex(valuation => valuation.PropertyId)
            .HasDatabaseName("IX_PropertyValuations_PropertyId");
        builder.HasIndex(valuation => valuation.ValuationDate)
            .HasDatabaseName("IX_PropertyValuations_ValuationDate");
    }
}
