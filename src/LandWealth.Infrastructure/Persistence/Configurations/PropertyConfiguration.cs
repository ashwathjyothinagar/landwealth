using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class PropertyConfiguration : AuditableEntityConfiguration<Property>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");
        builder.Ignore(property => property.LatestEstimatedValue);

        builder.Property(property => property.Name).HasMaxLength(200).IsRequired();
        builder.Property(property => property.PropertyType).HasStringEnumConversion(50);
        builder.Property(property => property.Status).HasStringEnumConversion(50);
        builder.Property(property => property.PrimarySurveyNumber).HasMaxLength(100);
        builder.Property(property => property.PurchasePrice).IsRequired().HasDefaultValue(0m);
        builder.Property(property => property.Location).HasMaxLength(255);
        builder.Property(property => property.Village).HasMaxLength(100);
        builder.Property(property => property.Taluk).HasMaxLength(100);
        builder.Property(property => property.District).HasMaxLength(100);
        builder.Property(property => property.State).HasMaxLength(100).IsRequired();
        builder.Property(property => property.Country).HasMaxLength(100).IsRequired().HasDefaultValue("India");
        builder.Property(property => property.PostalCode).HasMaxLength(20);
        builder.Property(property => property.Latitude).HasPrecision(10, 7);
        builder.Property(property => property.Longitude).HasPrecision(10, 7);
        builder.Property(property => property.Notes).HasColumnType("text");

        builder.HasIndex(property => new { property.UserId, property.Status })
            .HasDatabaseName("IX_Properties_UserId_Status");

        builder.HasMany(property => property.Parcels)
            .WithOne(parcel => parcel.Property)
            .HasForeignKey(parcel => parcel.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(property => property.Owners)
            .WithOne(owner => owner.Property)
            .HasForeignKey(owner => owner.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(property => property.Valuations)
            .WithOne(valuation => valuation.Property)
            .HasForeignKey(valuation => valuation.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(property => property.Documents)
            .WithOne(document => document.Property)
            .HasForeignKey(document => document.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(property => property.Reminders)
            .WithOne(reminder => reminder.Property)
            .HasForeignKey(reminder => reminder.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
