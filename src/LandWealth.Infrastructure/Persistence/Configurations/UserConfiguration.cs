using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Ignore(user => user.DomainEvents);

        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(user => user.FullName).HasMaxLength(150).IsRequired();
        builder.Property(user => user.PreferredCurrency).HasMaxLength(3).IsRequired().HasDefaultValue("INR");
        builder.Property(user => user.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(user => user.CreatedAt).IsRequired();
    }
}
