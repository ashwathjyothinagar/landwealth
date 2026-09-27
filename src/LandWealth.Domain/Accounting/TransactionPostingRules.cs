using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Accounting;

public readonly record struct PostingLine(
    TransactionLineType LineType,
    decimal Amount,
    Guid? AccountId,
    AccountType? AccountType,
    Guid? PropertyId,
    CategoryType? CategoryType);

public static class TransactionPostingRules
{
    public static void Ensure(TransactionType type, Guid? propertyId, IReadOnlyCollection<PostingLine> lines)
    {
        if (lines.Count < 2)
            throw new DomainException("A transaction must have at least two lines.");

        var debits = lines.Where(line => line.LineType == TransactionLineType.Debit).Sum(line => line.Amount);
        var credits = lines.Where(line => line.LineType == TransactionLineType.Credit).Sum(line => line.Amount);
        if (debits != credits)
            throw new DomainException("Debits must equal credits.");

        switch (type)
        {
            case TransactionType.Adjustment:
            case TransactionType.Reversal:
                return;
            case TransactionType.Income:
                RequireNoProperty(propertyId, type);
                RequireIncome(lines);
                return;
            case TransactionType.Expense:
                RequireNoProperty(propertyId, type);
                RequireExpense(lines);
                return;
            case TransactionType.Transfer:
                RequireNoProperty(propertyId, type);
                RequireTransfer(lines);
                return;
            case TransactionType.PropertyPurchase:
                RequireProperty(propertyId);
                RequirePropertySides(lines, CategoryType.CapEx_Acquisition, CategoryType.CapEx_Acquisition);
                return;
            case TransactionType.PropertyExpense:
                RequireProperty(propertyId);
                RequirePropertySides(lines, CategoryType.CapEx_Improvement, CategoryType.OpEx_Maintenance);
                return;
            case TransactionType.PropertyIncome:
                RequireProperty(propertyId);
                RequirePropertyIncome(lines);
                return;
            case TransactionType.PropertySale:
                RequireProperty(propertyId);
                if (!lines.Any(line => line.LineType == TransactionLineType.Debit && IsCashAccount(line.AccountType)))
                    throw new DomainException("A property sale debits the bank or cash account that receives the proceeds.");
                return;
            case TransactionType.Investment:
                RequireNoProperty(propertyId, type);
                RequireInvestment(lines);
                return;
            case TransactionType.LiabilityPayment:
                RequireNoProperty(propertyId, type);
                RequireLiabilityPayment(lines);
                return;
            default:
                throw new DomainException("This transaction type cannot be posted.");
        }
    }

    private static void RequireIncome(IReadOnlyCollection<PostingLine> lines)
    {
        var (debit, credit) = RequirePair(lines);
        if (!IsCashAccount(debit.AccountType))
            throw new DomainException("Income debits a bank, cash, or other liquid account.");
        if (credit.AccountId is not null || credit.CategoryType != CategoryType.Income)
            throw new DomainException("Income credits an income category.");
    }

    private static void RequireExpense(IReadOnlyCollection<PostingLine> lines)
    {
        var (debit, credit) = RequirePair(lines);
        if (debit.AccountId is not null || debit.CategoryType != CategoryType.Expense)
            throw new DomainException("An expense debits an expense category.");
        if (!IsFundingAccount(credit.AccountType))
            throw new DomainException("An expense credits the bank, cash, or credit card that paid it.");
    }

    private static void RequireTransfer(IReadOnlyCollection<PostingLine> lines)
    {
        var (debit, credit) = RequirePair(lines);
        if (debit.AccountId is null || credit.AccountId is null || debit.AccountId == credit.AccountId)
            throw new DomainException("A transfer moves money between two different accounts.");
        if (!IsCashAccount(debit.AccountType) || !IsCashAccount(credit.AccountType))
            throw new DomainException("Transfers move money between bank, cash, and other liquid accounts.");
        if (lines.Any(line => line.CategoryType is CategoryType.Income or CategoryType.Expense
                or CategoryType.CapEx_Acquisition or CategoryType.CapEx_Improvement or CategoryType.OpEx_Maintenance))
            throw new DomainException("A transfer is not income, expense, or a property cost.");
    }

    private static void RequirePropertySides(IReadOnlyCollection<PostingLine> lines, CategoryType first, CategoryType second)
    {
        var debits = lines.Where(line => line.LineType == TransactionLineType.Debit).ToList();
        if (debits.Count == 0 || debits.Any(line => line.CategoryType != first && line.CategoryType != second))
            throw new DomainException("The property debit uses the category for this transaction.");
        if (!lines.Any(line => line.LineType == TransactionLineType.Credit && IsFundingAccount(line.AccountType)))
            throw new DomainException("The payment credits a bank, cash, or credit card.");
    }

    private static void RequirePropertyIncome(IReadOnlyCollection<PostingLine> lines)
    {
        if (!lines.Any(line => line.LineType == TransactionLineType.Debit && IsCashAccount(line.AccountType)))
            throw new DomainException("Property income debits the bank or cash account that received it.");
        if (!lines.Any(line => line.LineType == TransactionLineType.Credit && line.CategoryType == CategoryType.Income))
            throw new DomainException("Property income credits an income category.");
    }

    private static void RequireInvestment(IReadOnlyCollection<PostingLine> lines)
    {
        var (debit, credit) = RequirePair(lines);
        if (debit.AccountType != AccountType.OtherFinancialAccount)
            throw new DomainException("An investment debits the investment account.");
        if (!IsCashAccount(credit.AccountType))
            throw new DomainException("An investment credits the bank or cash account that funded it.");
    }

    private static void RequireLiabilityPayment(IReadOnlyCollection<PostingLine> lines)
    {
        var (debit, credit) = RequirePair(lines);
        if (debit.AccountType != AccountType.CreditCard)
            throw new DomainException("A liability payment debits the credit card.");
        if (!IsCashAccount(credit.AccountType))
            throw new DomainException("A liability payment credits the bank or cash account that paid it.");
    }

    private static (PostingLine Debit, PostingLine Credit) RequirePair(IReadOnlyCollection<PostingLine> lines)
    {
        var debits = lines.Where(line => line.LineType == TransactionLineType.Debit).ToList();
        var credits = lines.Where(line => line.LineType == TransactionLineType.Credit).ToList();
        if (lines.Count != 2 || debits.Count != 1 || credits.Count != 1)
            throw new DomainException("This transaction uses one debit and one credit.");
        return (debits[0], credits[0]);
    }

    private static void RequireProperty(Guid? propertyId)
    {
        if (propertyId is null)
            throw new DomainException("This transaction is linked to a property.");
    }

    private static void RequireNoProperty(Guid? propertyId, TransactionType type)
    {
        if (propertyId is not null)
            throw new DomainException($"{type} is not posted against a property.");
    }

    private static bool IsCashAccount(AccountType? accountType)
        => accountType is AccountType.BankAccount or AccountType.CashAccount or AccountType.OtherFinancialAccount;

    private static bool IsFundingAccount(AccountType? accountType)
        => IsCashAccount(accountType) || accountType == AccountType.CreditCard;
}
