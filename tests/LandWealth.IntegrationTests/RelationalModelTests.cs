using FluentAssertions;
using LandWealth.Domain.Entities;
using LandWealth.Infrastructure;
using LandWealth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LandWealth.IntegrationTests;

public class RelationalModelTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        DependencyInjection.ConfigureMySql(
            options,
            "Server=localhost;Port=3306;Database=landwealth;User=landwealth_user;Password=landwealth_password_123;");
        return new ApplicationDbContext(options.Options);
    }

    [Fact]
    public void Model_UsesExactDecimalTypes_AndSeedsSystemCategories()
    {
        using var context = CreateContext();
        var model = context.Model;

        var expectedTables = new[]
        {
            "Users", "Properties", "PropertyParcels", "PropertyOwners", "Accounts", "Categories",
            "Transactions", "TransactionLines", "PropertyValuations", "PropertyDocuments",
            "PropertyReminders", "Assets", "Liabilities", "AuditLogs"
        };

        var tableNames = model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(name => name is not null)
            .ToHashSet();

        tableNames.Should().Contain(expectedTables);

        ColumnType(model, typeof(Account), nameof(Account.OpeningBalance)).Should().Be("decimal(18,4)");
        ColumnType(model, typeof(Transaction), nameof(Transaction.Amount)).Should().Be("decimal(18,4)");
        ColumnType(model, typeof(Property), nameof(Property.PurchasePrice)).Should().Be("decimal(18,4)");
        ColumnType(model, typeof(PropertyOwner), nameof(PropertyOwner.OwnershipPercentage)).Should().Be("decimal(5,2)");
        ColumnType(model, typeof(Liability), nameof(Liability.InterestRate)).Should().Be("decimal(5,2)");
        ColumnType(model, typeof(Property), nameof(Property.Latitude)).Should().Be("decimal(10,7)");
        ColumnType(model, typeof(User), nameof(User.Id)).Should().Be("char(36)");

        var monetaryColumns = model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
            .Select(property => property.GetColumnType())
            .ToList();

        monetaryColumns.Should().OnlyContain(columnType =>
            columnType == "decimal(18,4)" || columnType == "decimal(5,2)" || columnType == "decimal(10,7)");

        context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Category))!
            .GetSeedData()
            .Should()
            .HaveCount(20);

        var script = context.Database.GenerateCreateScript();
        script.Should().Contain("decimal(18,4)");
        script.Should().Contain("utf8mb4");
        script.ToLowerInvariant().Should().NotContain("double");
        script.ToLowerInvariant().Should().NotContain("float");
    }

    private static string ColumnType(Microsoft.EntityFrameworkCore.Metadata.IModel model, Type entityType, string propertyName)
    {
        var property = model.FindEntityType(entityType)!.FindProperty(propertyName);
        property.Should().NotBeNull();
        return property!.GetColumnType();
    }
}
