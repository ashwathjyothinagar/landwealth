using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class TransactionLineConfiguration : IEntityTypeConfiguration<TransactionLine>
{
    public void Configure(EntityTypeBuilder<TransactionLine> builder)
    {
        builder.ToTable("TransactionLines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever();

        builder.Property(line => line.LineType).HasStringEnumConversion(10);
        builder.Property(line => line.Amount).IsRequired();
        builder.Property(line => line.Memo).HasMaxLength(255);

        builder.HasIndex(line => line.TransactionId)
            .HasDatabaseName("IX_TransactionLines_TransactionId");
        builder.HasIndex(line => line.AccountId)
            .HasDatabaseName("IX_TransactionLines_AccountId");

        builder.HasOne(line => line.Account)
            .WithMany()
            .HasForeignKey(line => line.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.Property)
            .WithMany()
            .HasForeignKey(line => line.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.Category)
            .WithMany()
            .HasForeignKey(line => line.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
