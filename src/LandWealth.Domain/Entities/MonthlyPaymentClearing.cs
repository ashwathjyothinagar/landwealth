using LandWealth.Domain.Common;

namespace LandWealth.Domain.Entities;

public class MonthlyPaymentClearing : AuditableEntity<Guid>
{
    private MonthlyPaymentClearing() { }

    public Guid MonthlyPaymentId { get; private set; }
    public Guid TransactionId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public DateOnly PaidOn { get; private set; }
    public decimal Amount { get; private set; }

    public static MonthlyPaymentClearing Record(
        Guid userId,
        Guid monthlyPaymentId,
        Guid transactionId,
        int year,
        int month,
        DateOnly paidOn,
        decimal amount,
        Guid createdBy)
    {
        return new MonthlyPaymentClearing
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MonthlyPaymentId = monthlyPaymentId,
            TransactionId = transactionId,
            Year = year,
            Month = month,
            PaidOn = paidOn,
            Amount = amount,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }
}
