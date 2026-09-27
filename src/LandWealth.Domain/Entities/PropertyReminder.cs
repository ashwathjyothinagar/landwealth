using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Entities;

public class PropertyReminder : AuditableEntity<Guid>
{
    private PropertyReminder() { }

    public Guid? PropertyId { get; private set; }
    public Property? Property { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateOnly DueDate { get; private set; }
    public ReminderPriority Priority { get; private set; }
    public ReminderStatus Status { get; private set; }
    public DateOnly? CompletedDate { get; private set; }

    public bool IsOverdue => Status == ReminderStatus.Pending && DueDate < DateOnly.FromDateTime(DateTime.UtcNow);

    public static PropertyReminder Create(
        Guid userId, string title, DateOnly dueDate,
        ReminderPriority priority, Guid createdBy,
        Guid? propertyId = null, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new PropertyReminder
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PropertyId = propertyId,
            Title = title.Trim(),
            Description = description?.Trim(),
            DueDate = dueDate,
            Priority = priority,
            Status = ReminderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Complete(DateOnly completedDate, Guid modifiedBy)
    {
        if (Status != ReminderStatus.Pending)
            throw new DomainException("Only a pending reminder can be completed.");

        Status = ReminderStatus.Completed;
        CompletedDate = completedDate;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Dismiss(Guid modifiedBy)
    {
        if (Status != ReminderStatus.Pending)
            throw new DomainException("Only a pending reminder can be dismissed.");

        Status = ReminderStatus.Dismissed;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void UpdateDueDate(DateOnly newDueDate, Guid modifiedBy)
    {
        DueDate = newDueDate;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
