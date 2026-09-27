using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Entities;

public class PropertyOwner : AuditableEntity<Guid>
{
    private PropertyOwner() { }

    public Guid PropertyId { get; private set; }
    public Property? Property { get; private set; }
    public string OwnerName { get; private set; } = default!;
    public decimal OwnershipPercentage { get; private set; }
    public OwnershipType OwnershipType { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string? Notes { get; private set; }

    public bool IsActive => EndDate == null || EndDate > DateOnly.FromDateTime(DateTime.UtcNow);

    public static PropertyOwner Create(
        Guid propertyId, Guid userId,
        string ownerName, decimal percentage,
        OwnershipType ownershipType, DateOnly startDate,
        Guid createdBy, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);
        if (percentage <= 0 || percentage > 100)
            throw new DomainException("Ownership percentage must be between 0.01 and 100.");

        return new PropertyOwner
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            UserId = userId,
            OwnerName = ownerName.Trim(),
            OwnershipPercentage = percentage,
            OwnershipType = ownershipType,
            StartDate = startDate,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Transfer(DateOnly endDate, Guid modifiedBy)
    {
        if (endDate < StartDate)
            throw new DomainException("End date cannot be before start date.");
        EndDate = endDate;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
