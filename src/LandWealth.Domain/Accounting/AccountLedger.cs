using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Accounting;

public static class AccountLedger
{
    public static decimal SignedEffect(AccountType accountType, TransactionLineType lineType, decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Line amount must be greater than zero.");

        var increasesBalance = accountType == AccountType.CreditCard
            ? lineType == TransactionLineType.Credit
            : lineType == TransactionLineType.Debit;

        return increasesBalance ? amount : -amount;
    }

    public static IReadOnlyList<decimal> BalancesAfter(
        AccountType accountType,
        decimal openingBalance,
        IEnumerable<(TransactionLineType LineType, decimal Amount)> lines)
    {
        var balances = new List<decimal>();
        var balance = openingBalance;
        foreach (var line in lines)
        {
            balance += SignedEffect(accountType, line.LineType, line.Amount);
            balances.Add(balance);
        }

        return balances;
    }
}
