using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : AuditableEntityConfiguration<Account>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.Property(account => account.Name).HasMaxLength(150).IsRequired();
        builder.Property(account => account.AccountType).HasStringEnumConversion(50);
        builder.Property(account => account.Institution).HasMaxLength(100);
        builder.Property(account => account.MaskedAccountNumber).HasMaxLength(30);
        builder.Property(account => account.OpeningBalance).IsRequired();
        builder.Property(account => account.CurrentBalance).IsRequired();
        builder.Property(account => account.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("INR");
        builder.Property(account => account.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(account => account.Notes).HasColumnType("text");

        builder.HasIndex(account => new { account.UserId, account.IsActive })
            .HasDatabaseName("IX_Accounts_UserId_IsActive");
    }
}
