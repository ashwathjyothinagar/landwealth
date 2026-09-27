using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.ValueObjects;

/// <summary>
/// Land measurement stored in the unit written on the survey document.
/// <see cref="InAcres"/> is a reporting conversion only; the original value is never rewritten.
/// Bigha varies by state, so that factor is an approximation and must not be treated as a legal conversion.
/// </summary>
public sealed class Extent : ValueObject
{
    private static readonly Dictionary<ExtentUnit, decimal> ToAcresFactor = new()
    {
        [ExtentUnit.Acres] = 1m,
        [ExtentUnit.Guntas] = 1m / 40m,
        [ExtentUnit.Cents] = 1m / 100m,
        [ExtentUnit.SqFt] = 1m / 43560m,
        [ExtentUnit.SqYards] = 1m / 4840m,
        [ExtentUnit.SqMeters] = 1m / 4046.856m,
        [ExtentUnit.Hectares] = 2.47105m,
        [ExtentUnit.Bigha] = 0.6198m
    };

    public decimal Value { get; private set; }
    public ExtentUnit Unit { get; private set; }

    private Extent()
    {
    }

    public Extent(decimal value, ExtentUnit unit)
    {
        if (value <= 0)
            throw new DomainException("Parcel extent must be greater than zero.");

        Value = value;
        Unit = unit;
    }

    public decimal InAcres => Value * ToAcresFactor[Unit];

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Unit;
    }
}
