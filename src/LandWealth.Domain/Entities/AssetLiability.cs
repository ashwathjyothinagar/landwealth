using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Entities;

public class Asset : AuditableEntity<Guid>
{
    private Asset() { }

    public string Name { get; private set; } = default!;
    public AssetType AssetType { get; private set; }
    public decimal EstimatedValue { get; private set; }
    public decimal AcquisitionCost { get; private set; }
    public DateOnly? AcquisitionDate { get; private set; }
    public string? Notes { get; private set; }

    public decimal UnrealizedGain => EstimatedValue - AcquisitionCost;

    public static Asset Create(
        Guid userId, string name, AssetType assetType,
        decimal acquisitionCost, decimal estimatedValue,
        Guid createdBy, DateOnly? acquisitionDate = null, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (acquisitionCost < 0) throw new DomainException("Acquisition cost cannot be negative.");
        if (estimatedValue < 0) throw new DomainException("Estimated value cannot be negative.");

        return new Asset
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            AssetType = assetType,
            AcquisitionCost = acquisitionCost,
            EstimatedValue = estimatedValue,
            AcquisitionDate = acquisitionDate,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void UpdateValue(decimal newEstimatedValue, Guid modifiedBy)
    {
        if (newEstimatedValue < 0) throw new DomainException("Estimated value cannot be negative.");
        EstimatedValue = newEstimatedValue;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}

public class Liability : AuditableEntity<Guid>
{
    private Liability() { }

    public string Name { get; private set; } = default!;
    public LiabilityType LiabilityType { get; private set; }
    public string? Lender { get; private set; }
    public decimal PrincipalAmount { get; private set; }
    public decimal OutstandingBalance { get; private set; }
    public decimal? InterestRate { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string? Notes { get; private set; }

    public static Liability Create(
        Guid userId, string name, LiabilityType liabilityType,
        decimal principalAmount, Guid createdBy,
        string? lender = null, decimal? interestRate = null,
        DateOnly? startDate = null, DateOnly? endDate = null, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (principalAmount < 0) throw new DomainException("Principal amount cannot be negative.");

        return new Liability
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            LiabilityType = liabilityType,
            Lender = lender?.Trim(),
            PrincipalAmount = principalAmount,
            OutstandingBalance = principalAmount,
            InterestRate = interestRate,
            StartDate = startDate,
            EndDate = endDate,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void ApplyPayment(decimal principalPaid, Guid modifiedBy)
    {
        if (principalPaid < 0) throw new DomainException("Payment amount cannot be negative.");
        OutstandingBalance = Math.Max(0, OutstandingBalance - principalPaid);
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
