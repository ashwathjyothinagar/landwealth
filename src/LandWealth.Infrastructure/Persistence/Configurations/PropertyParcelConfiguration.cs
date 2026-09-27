using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class PropertyParcelConfiguration : AuditableEntityConfiguration<PropertyParcel>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PropertyParcel> builder)
    {
        builder.ToTable("PropertyParcels");
        builder.Ignore(parcel => parcel.ExtentInAcres);

        builder.Property(parcel => parcel.SurveyNumber).HasMaxLength(100).IsRequired();
        builder.Property(parcel => parcel.SubdivisionNumber).HasMaxLength(50);
        builder.Property(parcel => parcel.OwnershipPercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(parcel => parcel.Status).HasStringEnumConversion(50);
        builder.Property(parcel => parcel.BoundaryDescription).HasColumnType("text");
        builder.Property(parcel => parcel.Notes).HasColumnType("text");

        builder.OwnsOne(parcel => parcel.Extent, extent =>
        {
            extent.Property(value => value.Value)
                .HasColumnName("Extent")
                .HasPrecision(18, 4)
                .IsRequired();
            extent.Property(value => value.Unit)
                .HasColumnName("ExtentUnit")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            extent.Ignore(value => value.InAcres);
        });
        builder.Navigation(parcel => parcel.Extent).IsRequired();

        builder.HasIndex(parcel => parcel.PropertyId)
            .HasDatabaseName("IX_PropertyParcels_PropertyId");
    }
}
