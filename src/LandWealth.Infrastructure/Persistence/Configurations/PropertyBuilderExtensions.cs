using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LandWealth.Infrastructure.Persistence.Configurations;

internal static class PropertyBuilderExtensions
{
    public static PropertyBuilder<TEnum> HasStringEnumConversion<TEnum>(this PropertyBuilder<TEnum> property, int maxLength)
        where TEnum : struct, Enum
        => property.HasConversion<string>().HasMaxLength(maxLength).IsRequired();
}
