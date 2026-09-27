using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;
using LandWealth.Domain.ValueObjects;

namespace LandWealth.Domain.Entities;

public class PropertyParcel : AuditableEntity<Guid>
{
    private PropertyParcel() { }

    public Guid PropertyId { get; private set; }
    public Property? Property { get; private set; }
    public string SurveyNumber { get; private set; } = default!;
    public string? SubdivisionNumber { get; private set; }
    public Extent Extent { get; private set; } = default!;
    public string? BoundaryDescription { get; private set; }
    public decimal OwnershipPercentage { get; private set; }
    public ParcelStatus Status { get; private set; }
    public string? Notes { get; private set; }

    public decimal ExtentInAcres => Extent.InAcres;

    public static PropertyParcel Create(
        Guid propertyId,
        Guid userId,
        string surveyNumber,
        decimal extent,
        ExtentUnit unit,
        Guid createdBy,
        string? subdivisionNumber = null,
        decimal ownershipPercentage = 100m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surveyNumber);
        EnsureOwnershipPercentage(ownershipPercentage);

        return new PropertyParcel
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            UserId = userId,
            SurveyNumber = surveyNumber.Trim(),
            SubdivisionNumber = subdivisionNumber?.Trim(),
            Extent = new Extent(extent, unit),
            OwnershipPercentage = ownershipPercentage,
            Status = ParcelStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Update(
        string surveyNumber, string? subdivisionNumber,
        decimal extent, ExtentUnit unit,
        decimal ownershipPercentage, string? boundaryDescription, string? notes,
        Guid modifiedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surveyNumber);
        EnsureOwnershipPercentage(ownershipPercentage);
        SurveyNumber = surveyNumber.Trim();
        SubdivisionNumber = subdivisionNumber?.Trim();
        Extent = new Extent(extent, unit);
        OwnershipPercentage = ownershipPercentage;
        BoundaryDescription = boundaryDescription?.Trim();
        Notes = notes?.Trim();
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void MarkSold(Guid modifiedBy) => SetStatus(ParcelStatus.Sold, modifiedBy);

    public void MarkSubdivided(Guid modifiedBy) => SetStatus(ParcelStatus.Subdivided, modifiedBy);

    private void SetStatus(ParcelStatus status, Guid modifiedBy)
    {
        Status = status;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    private static void EnsureOwnershipPercentage(decimal ownershipPercentage)
    {
        if (ownershipPercentage <= 0 || ownershipPercentage > 100)
            throw new DomainException("Ownership percentage must be between 0.01 and 100.");
    }
}
