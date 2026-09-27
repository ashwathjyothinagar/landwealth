using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;

namespace LandWealth.Domain.Entities;

public class Category : Entity<Guid>
{
    private Category() { }

    /// <summary>Null = system-level category visible to all users. Non-null = user-specific custom category.</summary>
    public Guid? UserId { get; private set; }
    public string Name { get; private set; } = default!;
    public CategoryType CategoryType { get; private set; }
    public string? Description { get; private set; }
    /// <summary>System categories cannot be modified or deleted.</summary>
    public bool IsSystem { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private set; }

    public static Category CreateSystem(string name, CategoryType type, string? description = null, Guid? parentId = null, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category
        {
            Id = id ?? Guid.NewGuid(),
            UserId = null,
            Name = name.Trim(),
            CategoryType = type,
            Description = description,
            IsSystem = true,
            ParentCategoryId = parentId
        };
    }

    public static Category CreateUserDefined(Guid userId, string name, CategoryType type, string? description = null, Guid? parentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            CategoryType = type,
            Description = description,
            IsSystem = false,
            ParentCategoryId = parentId
        };
    }
}
