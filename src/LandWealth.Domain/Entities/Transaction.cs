using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Events;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Entities;

/// <summary>
/// Transaction header — represents one financial business event.
/// Every Transaction must contain at least two TransactionLines (double-entry).
/// Sum of Debits MUST equal Sum of Credits before posting.
/// </summary>
public class Transaction : AuditableEntity<Guid>
{
    private readonly List<TransactionLine> _lines = new();

    private Transaction() { }

    public DateOnly TransactionDate { get; private set; }
    public TransactionType TransactionType { get; private set; }
    /// <summary>Total positive amount of the event (e.g. ₹50,000 paid for fencing).</summary>
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = default!;
    public TransactionStatus Status { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public Guid? PropertyId { get; private set; }
    public Property? Property { get; private set; }
    public Guid? ParcelId { get; private set; }
    public PropertyParcel? Parcel { get; private set; }
    /// <summary>Set when this is a reversal of another transaction.</summary>
    public Guid? ReversedTransactionId { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<TransactionLine> Lines => _lines.AsReadOnly();

    public decimal TotalDebits => _lines.Where(l => l.LineType == TransactionLineType.Debit).Sum(l => l.Amount);
    public decimal TotalCredits => _lines.Where(l => l.LineType == TransactionLineType.Credit).Sum(l => l.Amount);
    public bool IsBalanced => TotalDebits == TotalCredits;

    public static Transaction Create(
        Guid userId, DateOnly transactionDate, TransactionType type,
        decimal amount, string description, Guid createdBy,
        Guid? propertyId = null, Guid? parcelId = null,
        string? referenceNumber = null, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (amount <= 0) throw new DomainException("Transaction amount must be greater than zero.");

        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TransactionDate = transactionDate,
            TransactionType = type,
            Amount = amount,
            Description = description.Trim(),
            Status = TransactionStatus.Draft,
            PropertyId = propertyId,
            ParcelId = parcelId,
            ReferenceNumber = referenceNumber?.Trim(),
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public TransactionLine AddLine(
        TransactionLineType lineType,
        decimal amount,
        Guid? accountId = null,
        Guid? propertyId = null,
        Guid? categoryId = null,
        string? memo = null)
    {
        if (Status == TransactionStatus.Posted)
            throw new DomainException("Cannot add lines to a posted transaction.");
        if (amount <= 0) throw new DomainException("Line amount must be greater than zero.");

        var line = new TransactionLine(Id, lineType, amount, accountId, propertyId, categoryId, memo);
        _lines.Add(line);
        return line;
    }

    /// <summary>
    /// Posts the transaction after verifying it is balanced.
    /// Once posted, it is immutable — corrections must use Adjustment or Reversal.
    /// </summary>
    public void Post()
    {
        if (Status == TransactionStatus.Posted)
            throw new DomainException("Transaction is already posted.");
        if (_lines.Count < 2)
            throw new DomainException("A transaction must have at least two lines.");
        if (!IsBalanced)
            throw new DomainException(
                $"Transaction is not balanced. Debits: {TotalDebits}, Credits: {TotalCredits}. " +
                "Total Debits must equal Total Credits before posting.");
        if (TotalDebits != Amount)
            throw new DomainException("The transaction amount must equal the total of the debit lines.");

        Status = TransactionStatus.Posted;
        AddDomainEvent(new TransactionPostedEvent(Id, UserId, Amount, TransactionType));
    }

    /// <summary>
    /// Marks this transaction as reversed (called when a reversal entry is created).
    /// The original transaction is preserved but flagged as Reversed.
    /// </summary>
    public void MarkReversed(Guid modifiedBy)
    {
        if (Status != TransactionStatus.Posted)
            throw new DomainException("Only posted transactions can be reversed.");

        Status = TransactionStatus.Reversed;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    /// <summary>Creates a mirror Reversal transaction for this one.</summary>
    public Transaction CreateReversal(Guid createdBy, string reversalDescription)
    {
        if (Status != TransactionStatus.Posted)
            throw new DomainException("Only posted transactions can be reversed.");
        if (TransactionType == TransactionType.Reversal)
            throw new DomainException("A reversal stays in the ledger. Correct it with an adjustment.");

        var reversal = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TransactionType = TransactionType.Reversal,
            Amount = Amount,
            Description = reversalDescription.Trim(),
            Status = TransactionStatus.Draft,
            PropertyId = PropertyId,
            ParcelId = ParcelId,
            ReversedTransactionId = Id,
            Notes = $"Reversal of transaction {Id}",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        // Mirror all lines (swap Debit ↔ Credit)
        foreach (var originalLine in _lines)
        {
            var mirroredType = originalLine.LineType == TransactionLineType.Debit
                ? TransactionLineType.Credit
                : TransactionLineType.Debit;

            reversal._lines.Add(new TransactionLine(
                reversal.Id, mirroredType, originalLine.Amount,
                originalLine.AccountId, originalLine.PropertyId,
                originalLine.CategoryId, $"Reversal: {originalLine.Memo}"));
        }

        return reversal;
    }
}

public class TransactionLine : Entity<Guid>
{
    private TransactionLine() { }

    internal TransactionLine(
        Guid transactionId,
        TransactionLineType lineType,
        decimal amount,
        Guid? accountId,
        Guid? propertyId,
        Guid? categoryId,
        string? memo)
    {
        Id = Guid.NewGuid();
        TransactionId = transactionId;
        LineType = lineType;
        Amount = amount;
        AccountId = accountId;
        PropertyId = propertyId;
        CategoryId = categoryId;
        Memo = memo?.Trim();
    }

    public Guid TransactionId { get; private set; }
    public Transaction? Transaction { get; private set; }
    public Guid? AccountId { get; private set; }
    public Account? Account { get; private set; }
    public Guid? PropertyId { get; private set; }
    public Property? Property { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public TransactionLineType LineType { get; private set; }
    public decimal Amount { get; private set; }
    public string? Memo { get; private set; }
}
