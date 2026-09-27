using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Entities;

/// <summary>
/// Historical property valuation record.
/// IMPORTANT: Valuations are NOT financial transactions.
/// They do NOT modify any bank or cash account balance.
/// They are used to compute Unrealized Gain = EstimatedValue - Cost Basis.
/// </summary>
public class PropertyValuation : AuditableEntity<Guid>
{
    private PropertyValuation() { }

    public Guid PropertyId { get; private set; }
    public Property? Property { get; private set; }
    public DateOnly ValuationDate { get; private set; }
    public decimal EstimatedValue { get; private set; }
    public ValuationSource ValuationSource { get; private set; }
    public string? Notes { get; private set; }

    public static PropertyValuation Create(
        Guid propertyId, Guid userId,
        DateOnly valuationDate, decimal estimatedValue,
        ValuationSource source, Guid createdBy, string? notes = null)
    {
        if (estimatedValue < 0)
            throw new DomainException("Estimated value cannot be negative.");

        return new PropertyValuation
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            UserId = userId,
            ValuationDate = valuationDate,
            EstimatedValue = estimatedValue,
            ValuationSource = source,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }
}
