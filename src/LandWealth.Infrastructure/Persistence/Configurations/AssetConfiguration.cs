using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class AssetConfiguration : AuditableEntityConfiguration<Asset>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");
        builder.Ignore(asset => asset.UnrealizedGain);

        builder.Property(asset => asset.Name).HasMaxLength(150).IsRequired();
        builder.Property(asset => asset.AssetType).HasStringEnumConversion(50);
        builder.Property(asset => asset.EstimatedValue).IsRequired();
        builder.Property(asset => asset.AcquisitionCost).IsRequired();
        builder.Property(asset => asset.Notes).HasColumnType("text");
    }
}
