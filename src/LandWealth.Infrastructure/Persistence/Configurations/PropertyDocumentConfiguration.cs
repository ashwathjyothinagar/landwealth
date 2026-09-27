using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class PropertyDocumentConfiguration : AuditableEntityConfiguration<PropertyDocument>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PropertyDocument> builder)
    {
        builder.ToTable("PropertyDocuments");

        builder.Property(document => document.DocumentType).HasStringEnumConversion(50);
        builder.Property(document => document.DocumentNumber).HasMaxLength(100);
        builder.Property(document => document.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(document => document.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.FileSize).IsRequired();
        builder.Property(document => document.StorageKey).HasMaxLength(255).IsRequired();
        builder.Property(document => document.Notes).HasColumnType("text");

        builder.HasIndex(document => document.PropertyId)
            .HasDatabaseName("IX_PropertyDocuments_PropertyId");

        builder.HasOne(document => document.Parcel)
            .WithMany()
            .HasForeignKey(document => document.ParcelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
