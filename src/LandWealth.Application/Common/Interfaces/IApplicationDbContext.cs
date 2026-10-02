using LandWealth.Domain.Entities;

namespace LandWealth.Application.Common.Interfaces;

/// <summary>
/// Persistence port for LandWealth. Infrastructure supplies the EF Core implementation.
/// Tenant query filters are applied in Phase 4.
/// </summary>
public interface IApplicationDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<Property> Properties { get; }
    IQueryable<PropertyParcel> PropertyParcels { get; }
    IQueryable<PropertyOwner> PropertyOwners { get; }
    IQueryable<Account> Accounts { get; }
    IQueryable<Category> Categories { get; }
    IQueryable<Transaction> Transactions { get; }
    IQueryable<TransactionLine> TransactionLines { get; }
    IQueryable<PropertyValuation> PropertyValuations { get; }
    IQueryable<PropertyDocument> PropertyDocuments { get; }
    IQueryable<PropertyReminder> PropertyReminders { get; }
    IQueryable<Asset> Assets { get; }
    IQueryable<Liability> Liabilities { get; }
    IQueryable<AuditLog> AuditLogs { get; }
    IQueryable<MonthlyPayment> MonthlyPayments { get; }
    IQueryable<MonthlyPaymentClearing> MonthlyPaymentClearings { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
