using LandWealth.Domain.Accounting;
using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Entities;

public class Account : AuditableEntity<Guid>
{
    private Account() { }

    public string Name { get; private set; } = default!;
    public AccountType AccountType { get; private set; }
    public string? Institution { get; private set; }
    /// <summary>Stored masked: only last 4 digits. e.g. •••• 4821</summary>
    public string? MaskedAccountNumber { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public string Currency { get; private set; } = "INR";
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    public static Account Create(
        Guid userId, string name, AccountType accountType,
        decimal openingBalance, Guid createdBy,
        string? institution = null, string? lastFourDigits = null, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (openingBalance < 0)
            throw new DomainException("Opening balance cannot be negative.");

        var requiresMask = accountType is AccountType.BankAccount or AccountType.CreditCard;
        if (requiresMask && string.IsNullOrWhiteSpace(lastFourDigits))
            throw new DomainException("Bank accounts and credit cards store only the last 4 digits.");

        string? masked = null;
        if (!string.IsNullOrWhiteSpace(lastFourDigits))
        {
            if (lastFourDigits.Length != 4 || !lastFourDigits.All(char.IsDigit))
                throw new DomainException("Only the last 4 digits may be stored.");
            masked = $"•••• {lastFourDigits}";
        }

        return new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            AccountType = accountType,
            Institution = institution?.Trim(),
            MaskedAccountNumber = masked,
            OpeningBalance = openingBalance,
            CurrentBalance = openingBalance,
            Currency = "INR",
            IsActive = true,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void UpdateDetails(string name, string? institution, bool isActive, string? notes, Guid modifiedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Institution = institution?.Trim();
        IsActive = isActive;
        Notes = notes?.Trim();
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Applies one journal line to the running balance.
    /// Bank and cash are debit-normal: a debit increases cash and a credit decreases it.
    /// A credit card stores the amount owed: a credit increases it and a debit decreases it.
    /// </summary>
    public void ApplyJournalLine(TransactionLineType lineType, decimal amount)
    {
        if (!IsActive)
            throw new DomainException("Inactive accounts cannot accept new entries.");

        CurrentBalance += AccountLedger.SignedEffect(AccountType, lineType, amount);
        LastModifiedAt = DateTime.UtcNow;
    }
}
