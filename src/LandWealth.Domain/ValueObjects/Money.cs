using LandWealth.Domain.Common;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.ValueObjects;

/// <summary>
/// Exact monetary amount. Amount is a <see cref="decimal"/> so ledger math never uses binary floating point.
/// </summary>
public sealed class Money : ValueObject
{
    public const string DefaultCurrency = "INR";

    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code.");

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Inr(decimal amount) => new(amount);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
            throw new DomainException($"Cannot combine {Currency} with {other.Currency}.");
    }
}
