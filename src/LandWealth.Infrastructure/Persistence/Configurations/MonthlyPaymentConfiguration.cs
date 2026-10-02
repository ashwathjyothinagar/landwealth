using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class MonthlyPaymentConfiguration : AuditableEntityConfiguration<MonthlyPayment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MonthlyPayment> builder)
    {
        builder.ToTable("MonthlyPayments");
        builder.Property(payment => payment.Name).HasMaxLength(150).IsRequired();
        builder.Property(payment => payment.Kind).HasStringEnumConversion(20);
        builder.Property(payment => payment.Amount).IsRequired();
        builder.Property(payment => payment.DueDay).IsRequired();
        builder.Property(payment => payment.StartsOn).IsRequired();
        builder.Property(payment => payment.IsActive).IsRequired();
        builder.Property(payment => payment.Notes).HasColumnType("text");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(payment => payment.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(payment => payment.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(payment => new { payment.UserId, payment.IsActive })
            .HasDatabaseName("IX_MonthlyPayments_UserId_IsActive");
    }
}

internal sealed class MonthlyPaymentClearingConfiguration : AuditableEntityConfiguration<MonthlyPaymentClearing>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MonthlyPaymentClearing> builder)
    {
        builder.ToTable("MonthlyPaymentClearings");
        builder.Property(clearing => clearing.Year).IsRequired();
        builder.Property(clearing => clearing.Month).IsRequired();
        builder.Property(clearing => clearing.PaidOn).IsRequired();
        builder.Property(clearing => clearing.Amount).IsRequired();

        builder.HasOne<MonthlyPayment>()
            .WithMany()
            .HasForeignKey(clearing => clearing.MonthlyPaymentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(clearing => clearing.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(clearing => new { clearing.MonthlyPaymentId, clearing.Year, clearing.Month })
            .HasDatabaseName("IX_MonthlyPaymentClearings_Payment_Month");
    }
}
