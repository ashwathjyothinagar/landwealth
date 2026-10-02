using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Entities;

public class MonthlyPayment : AuditableEntity<Guid>
{
    private MonthlyPayment() { }

    public string Name { get; private set; } = default!;
    public MonthlyPaymentKind Kind { get; private set; }
    public decimal Amount { get; private set; }
    public int DueDay { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public int? TotalInstallments { get; private set; }
    public DateOnly StartsOn { get; private set; }
    public DateOnly? StoppedOn { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    public static MonthlyPayment Create(
        Guid userId,
        string name,
        MonthlyPaymentKind kind,
        decimal amount,
        int dueDay,
        Guid accountId,
        Guid categoryId,
        DateOnly startsOn,
        Guid createdBy,
        int? totalInstallments = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (amount <= 0)
            throw new DomainException("The payment amount must be greater than zero.");
        if (dueDay is < 1 or > 31)
            throw new DomainException("The due day must be from 1 to 31.");
        if (kind == MonthlyPaymentKind.Emi && totalInstallments is null or < 1)
            throw new DomainException("An EMI needs the number of installments.");
        if (kind == MonthlyPaymentKind.Bill)
            totalInstallments = null;
        if (totalInstallments is > 600)
            throw new DomainException("Installments cannot exceed 600.");

        return new MonthlyPayment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Kind = kind,
            Amount = amount,
            DueDay = dueDay,
            AccountId = accountId,
            CategoryId = categoryId,
            TotalInstallments = totalInstallments,
            StartsOn = startsOn,
            IsActive = true,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Stop(DateOnly on, Guid modifiedBy)
    {
        if (!IsActive)
            throw new DomainException("This payment is already stopped.");

        IsActive = false;
        StoppedOn = on;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
