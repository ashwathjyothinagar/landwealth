using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : AuditableEntityConfiguration<Transaction>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.Ignore(transaction => transaction.TotalDebits);
        builder.Ignore(transaction => transaction.TotalCredits);
        builder.Ignore(transaction => transaction.IsBalanced);

        builder.Property(transaction => transaction.TransactionDate).IsRequired();
        builder.Property(transaction => transaction.TransactionType).HasStringEnumConversion(50);
        builder.Property(transaction => transaction.Amount).IsRequired();
        builder.Property(transaction => transaction.Description).HasMaxLength(300).IsRequired();
        builder.Property(transaction => transaction.Status).HasStringEnumConversion(30);
        builder.Property(transaction => transaction.ReferenceNumber).HasMaxLength(100);
        builder.Property(transaction => transaction.Notes).HasColumnType("text");

        builder.HasIndex(transaction => new { transaction.UserId, transaction.TransactionDate })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Transactions_UserId_Date");

        builder.HasIndex(transaction => transaction.PropertyId)
            .HasDatabaseName("IX_Transactions_PropertyId");

        builder.HasOne(transaction => transaction.Property)
            .WithMany()
            .HasForeignKey(transaction => transaction.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transaction => transaction.Parcel)
            .WithMany()
            .HasForeignKey(transaction => transaction.ParcelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(transaction => transaction.ReversedTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(transaction => transaction.Lines)
            .WithOne(line => line.Transaction)
            .HasForeignKey(line => line.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
