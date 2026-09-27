using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Accounting;

public readonly record struct BasisLine(
    Guid? PropertyId,
    TransactionLineType LineType,
    decimal Amount,
    CategoryType? CategoryType);

public readonly record struct CashLine(
    AccountType AccountType,
    TransactionLineType LineType,
    decimal Amount,
    TransactionType TransactionType);

public readonly record struct CostBasisBreakdown(
    decimal Acquisition,
    decimal Improvements,
    decimal Maintenance,
    decimal CostBasis);

public static class PropertyAccounting
{
    public static CostBasisBreakdown Summarize(IEnumerable<BasisLine> lines, Guid propertyId)
    {
        decimal acquisition = 0m;
        decimal improvements = 0m;
        decimal maintenance = 0m;
        foreach (var line in lines)
        {
            if (line.PropertyId != propertyId || line.CategoryType is null)
                continue;

            var signed = line.LineType == TransactionLineType.Debit ? line.Amount : -line.Amount;
            switch (line.CategoryType.Value)
            {
                case CategoryType.CapEx_Acquisition:
                    acquisition += signed;
                    break;
                case CategoryType.CapEx_Improvement:
                    improvements += signed;
                    break;
                case CategoryType.OpEx_Maintenance:
                    maintenance += signed;
                    break;
            }
        }

        return new CostBasisBreakdown(acquisition, improvements, maintenance, acquisition + improvements);
    }

    public static decimal CostBasis(IEnumerable<BasisLine> lines, Guid propertyId)
        => Summarize(lines, propertyId).CostBasis;

    public static decimal? UnrealizedGain(decimal? estimatedMarketValue, decimal costBasis)
        => estimatedMarketValue is null ? null : estimatedMarketValue.Value - costBasis;

    public static decimal AllocatedCostBasis(decimal totalCostBasis, decimal extentSold, decimal totalExtent)
    {
        if (totalExtent <= 0)
            throw new DomainException("Total extent must be greater than zero.");
        if (extentSold <= 0 || extentSold > totalExtent)
            throw new DomainException("Extent sold must be greater than zero and no larger than the total extent.");

        return decimal.Round(totalCostBasis * (extentSold / totalExtent), 4, MidpointRounding.AwayFromZero);
    }

    public static decimal RealizedGain(decimal grossProceeds, decimal sellingExpenses, decimal allocatedCostBasis)
    {
        if (grossProceeds < 0 || sellingExpenses < 0)
            throw new DomainException("Proceeds and selling expenses cannot be negative.");

        return (grossProceeds - sellingExpenses) - allocatedCostBasis;
    }

    public static decimal LiquidNetWorth(IEnumerable<(AccountType Type, decimal Balance)> accounts)
    {
        decimal liquid = 0m;
        foreach (var (type, balance) in accounts)
        {
            if (type is AccountType.BankAccount or AccountType.CashAccount or AccountType.OtherFinancialAccount)
                liquid += balance;
            else if (type == AccountType.CreditCard)
                liquid -= balance;
        }

        return liquid;
    }

    public static (decimal Inflows, decimal Outflows) CashFlow(IEnumerable<CashLine> lines)
    {
        decimal inflows = 0m;
        decimal outflows = 0m;
        foreach (var line in lines)
        {
            if (line.TransactionType == TransactionType.Transfer)
                continue;
            if (line.AccountType is not (AccountType.BankAccount or AccountType.CashAccount or AccountType.OtherFinancialAccount))
                continue;

            if (line.LineType == TransactionLineType.Debit)
                inflows += line.Amount;
            else
                outflows += line.Amount;
        }

        return (inflows, outflows);
    }
}

public static class ReminderPolicy
{
    public static ReminderPriority Escalate(ReminderPriority priority, bool isOverdue)
    {
        if (!isOverdue)
            return priority;

        return priority < ReminderPriority.High ? ReminderPriority.High : priority;
    }

    public static ReminderStatus DisplayStatus(ReminderStatus status, bool isOverdue)
        => status == ReminderStatus.Pending && isOverdue ? ReminderStatus.Overdue : status;

    public static bool ShowOnDashboard(ReminderStatus status, DateOnly dueDate, DateOnly today)
    {
        if (status != ReminderStatus.Pending)
            return false;
        if (dueDate < today)
            return true;
        return dueDate <= today.AddDays(30);
    }
}
