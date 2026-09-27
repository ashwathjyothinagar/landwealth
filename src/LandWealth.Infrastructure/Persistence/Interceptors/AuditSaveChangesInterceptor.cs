using System.Text.Json;
using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Common;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LandWealth.Infrastructure.Persistence.Interceptors;

public sealed class AuditSaveChangesInterceptor(
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private static readonly HashSet<Type> AuditedTypes =
    [
        typeof(Transaction),
        typeof(Account),
        typeof(PropertyValuation),
        typeof(Asset),
        typeof(Liability)
    ];

    private static readonly string[] SensitiveNames = ["Password", "Secret", "Token", "StorageKey", "Hash"];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
            return;

        var now = clock.UtcNow;
        var userId = currentUser.IsAuthenticated ? currentUser.UserId : Guid.Empty;
        var http = httpContextAccessor.HttpContext;

        if (context.ChangeTracker.Entries<AuditLog>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit log entries are append-only.");

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity<Guid>>().ToList())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = now;
                if (entry.Entity.CreatedBy == Guid.Empty && userId != Guid.Empty)
                    entry.Entity.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = now;
                if (userId != Guid.Empty)
                    entry.Entity.LastModifiedBy = userId;
            }

            var name = entry.Metadata.ClrType.Name;
            if (!AuditedTypes.Contains(entry.Metadata.ClrType) || entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            var action = entry.State == EntityState.Added ? AuditAction.Insert : AuditAction.Update;
            if (entry.Entity is Transaction transaction
                && transaction.TransactionType == TransactionType.Reversal
                && entry.State == EntityState.Added)
                action = AuditAction.Reversal;
            else if (entry.State == EntityState.Modified && entry.Entity.IsDeleted)
                action = AuditAction.SoftDelete;

            context.Add(AuditLog.Record(
                name,
                entry.Entity.Id.ToString(),
                action,
                userId == Guid.Empty ? null : userId,
                entry.State == EntityState.Modified ? Serialize(entry.OriginalValues) : null,
                Serialize(entry.CurrentValues),
                http?.Connection.RemoteIpAddress?.ToString(),
                http?.Request.Headers.UserAgent.ToString()));
        }
    }

    private static string Serialize(Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues values)
    {
        var payload = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var property in values.Properties)
        {
            if (property.IsShadowProperty())
                continue;
            if (SensitiveNames.Any(name => property.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
                continue;
            payload[property.Name] = values[property]?.ToString();
        }

        return JsonSerializer.Serialize(payload);
    }
}
