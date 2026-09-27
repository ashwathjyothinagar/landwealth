using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class LiabilityConfiguration : AuditableEntityConfiguration<Liability>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Liability> builder)
    {
        builder.ToTable("Liabilities");

        builder.Property(liability => liability.Name).HasMaxLength(150).IsRequired();
        builder.Property(liability => liability.LiabilityType).HasStringEnumConversion(50);
        builder.Property(liability => liability.Lender).HasMaxLength(150);
        builder.Property(liability => liability.PrincipalAmount).IsRequired();
        builder.Property(liability => liability.OutstandingBalance).IsRequired();
        builder.Property(liability => liability.InterestRate).HasPrecision(5, 2);
        builder.Property(liability => liability.Notes).HasColumnType("text");
    }
}
