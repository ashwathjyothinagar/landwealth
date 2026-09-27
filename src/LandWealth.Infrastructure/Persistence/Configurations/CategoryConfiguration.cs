using LandWealth.Domain.Entities;
using LandWealth.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();

        builder.Property(category => category.Name).HasMaxLength(100).IsRequired();
        builder.Property(category => category.CategoryType).HasStringEnumConversion(50);
        builder.Property(category => category.Description).HasMaxLength(255);
        builder.Property(category => category.IsSystem).IsRequired();

        builder.HasOne(category => category.ParentCategory)
            .WithMany()
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(category => category.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(category => category.UserId)
            .HasDatabaseName("IX_Categories_UserId");

        builder.HasData(SystemCategorySeed.All.ToArray());
    }
}
