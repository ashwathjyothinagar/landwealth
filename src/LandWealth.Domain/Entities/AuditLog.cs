using LandWealth.Domain.Enums;

namespace LandWealth.Domain.Entities;

/// <summary>
/// Append-only security and financial change log. Rows are never updated or deleted.
/// </summary>
public class AuditLog
{
    private AuditLog()
    {
    }

    public long Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string EntityName { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public AuditAction Action { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public static AuditLog Record(
        string entityName,
        string entityId,
        AuditAction action,
        Guid? userId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        return new AuditLog
        {
            UserId = userId,
            EntityName = entityName.Trim(),
            EntityId = entityId.Trim(),
            Action = action,
            Timestamp = DateTime.UtcNow,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress?.Trim(),
            UserAgent = userAgent?.Trim()
        };
    }
}
