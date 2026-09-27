using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LandWealth.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser ?? UnscopedCurrentUser.Instance;
    }

    public Guid CurrentUserId => _currentUser.UserId;

    public DbSet<User> Users => Set<User>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyParcel> PropertyParcels => Set<PropertyParcel>();
    public DbSet<PropertyOwner> PropertyOwners => Set<PropertyOwner>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionLine> TransactionLines => Set<TransactionLine>();
    public DbSet<PropertyValuation> PropertyValuations => Set<PropertyValuation>();
    public DbSet<PropertyDocument> PropertyDocuments => Set<PropertyDocument>();
    public DbSet<PropertyReminder> PropertyReminders => Set<PropertyReminder>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Liability> Liabilities => Set<Liability>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    IQueryable<User> IApplicationDbContext.Users => Users;
    IQueryable<Property> IApplicationDbContext.Properties => Properties;
    IQueryable<PropertyParcel> IApplicationDbContext.PropertyParcels => PropertyParcels;
    IQueryable<PropertyOwner> IApplicationDbContext.PropertyOwners => PropertyOwners;
    IQueryable<Account> IApplicationDbContext.Accounts => Accounts;
    IQueryable<Category> IApplicationDbContext.Categories => Categories;
    IQueryable<Transaction> IApplicationDbContext.Transactions => Transactions;
    IQueryable<TransactionLine> IApplicationDbContext.TransactionLines => TransactionLines;
    IQueryable<PropertyValuation> IApplicationDbContext.PropertyValuations => PropertyValuations;
    IQueryable<PropertyDocument> IApplicationDbContext.PropertyDocuments => PropertyDocuments;
    IQueryable<PropertyReminder> IApplicationDbContext.PropertyReminders => PropertyReminders;
    IQueryable<Asset> IApplicationDbContext.Assets => Assets;
    IQueryable<Liability> IApplicationDbContext.Liabilities => Liabilities;
    IQueryable<AuditLog> IApplicationDbContext.AuditLogs => AuditLogs;

    void IApplicationDbContext.Add<TEntity>(TEntity entity) => Add(entity);
    void IApplicationDbContext.Update<TEntity>(TEntity entity) => Update(entity);
    void IApplicationDbContext.Remove<TEntity>(TEntity entity) => Remove(entity);

    public Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => Users.IgnoreQueryFilters().FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Guid>().HaveColumnType("char(36)");
        configurationBuilder.Properties<Guid?>().HaveColumnType("char(36)");
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime(6)");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("datetime(6)");
        configurationBuilder.Properties<DateOnly>().HaveColumnType("date");
        configurationBuilder.Properties<DateOnly?>().HaveColumnType("date");
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
        configurationBuilder.Properties<decimal?>().HavePrecision(18, 4);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCharSet("utf8mb4");
        modelBuilder.UseCollation("utf8mb4_unicode_ci");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ApplyTenantFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasQueryFilter(user => user.Id == CurrentUserId);
        modelBuilder.Entity<Property>().HasQueryFilter(entity => entity.UserId == CurrentUserId && !entity.IsDeleted);
        modelBuilder.Entity<PropertyParcel>().HasQueryFilter(entity => entity.UserId == CurrentUserId && !entity.IsDeleted);
        modelBuilder.Entity<PropertyOwner>().HasQueryFilter(entity => entity.UserId == CurrentUserId && !entity.IsDeleted);
        modelBuilder.Entity<Account>().HasQueryFilter(entity => entity.UserId == CurrentUserId && !entity.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(entity => entity.UserId == null || entity.UserId == CurrentUserId);
        modelBuilder.Entity<Transaction>().HasQueryFilter(entity => entity.UserId == CurrentUserId && !entity.IsDeleted);
        modelBuilder.Entity<TransactionLine>().HasQueryFilter(line => line.Transaction!.UserId == CurrentUserId && !line.Transaction.IsDeleted);
        modelBuilder.Entity<PropertyValuation>().HasQueryFilter(entity => entity.UserId == CurrentUserId && !entity.IsDeleted);
        modelBuilder.Entity<PropertyDocument>().HasQueryFilter(entity => entity.UserId == CurrentUserId && !entity.IsDeleted);
        modelBuilder.Entity<PropertyReminder>().HasQueryFilter(entity => entity.UserId == CurrentUserId && !entity.IsDeleted);
        modelBuilder.Entity<Asset>().HasQueryFilter(entity => entity.UserId == CurrentUserId && !entity.IsDeleted);
        modelBuilder.Entity<Liability>().HasQueryFilter(entity => entity.UserId == CurrentUserId && !entity.IsDeleted);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(entity => entity.UserId == CurrentUserId);
    }
}

internal sealed class UnscopedCurrentUser : ICurrentUserService
{
    public static readonly UnscopedCurrentUser Instance = new();
    public Guid UserId => Guid.Empty;
    public bool IsAuthenticated => false;
}
