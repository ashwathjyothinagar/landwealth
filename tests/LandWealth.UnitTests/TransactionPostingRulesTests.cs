using FluentAssertions;
using LandWealth.Domain.Accounting;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.UnitTests;

public class TransactionPostingRulesTests
{
    private static readonly Guid BankId = Guid.NewGuid();
    private static readonly Guid CashId = Guid.NewGuid();
    private static readonly Guid CardId = Guid.NewGuid();

    [Fact]
    public void Income_DebitsCash_AndCreditsAnIncomeCategory()
    {
        var act = () => TransactionPostingRules.Ensure(TransactionType.Income, null, [
            Line(TransactionLineType.Credit, BankId, AccountType.BankAccount, null),
            Line(TransactionLineType.Debit, null, null, CategoryType.Income)
        ]);
        act.Should().Throw<DomainException>();

        TransactionPostingRules.Ensure(TransactionType.Income, null, [
            Line(TransactionLineType.Debit, BankId, AccountType.BankAccount, null),
            Line(TransactionLineType.Credit, null, null, CategoryType.Income)
        ]);
    }

    [Fact]
    public void Transfer_MovesCashBetweenTwoLiquidAccounts()
    {
        TransactionPostingRules.Ensure(TransactionType.Transfer, null, [
            Line(TransactionLineType.Debit, CashId, AccountType.CashAccount, CategoryType.Transfer),
            Line(TransactionLineType.Credit, BankId, AccountType.BankAccount, CategoryType.Transfer)
        ]);

        var sameAccount = () => TransactionPostingRules.Ensure(TransactionType.Transfer, null, [
            Line(TransactionLineType.Debit, BankId, AccountType.BankAccount, null),
            Line(TransactionLineType.Credit, BankId, AccountType.BankAccount, null)
        ]);
        sameAccount.Should().Throw<DomainException>();

        var card = () => TransactionPostingRules.Ensure(TransactionType.Transfer, null, [
            Line(TransactionLineType.Debit, CardId, AccountType.CreditCard, null),
            Line(TransactionLineType.Credit, BankId, AccountType.BankAccount, null)
        ]);
        card.Should().Throw<DomainException>();
    }

    [Fact]
    public void PropertyPurchase_RequiresAPropertyAndAnAcquisitionDebit()
    {
        var missingProperty = () => TransactionPostingRules.Ensure(TransactionType.PropertyPurchase, null, [
            Line(TransactionLineType.Debit, null, null, CategoryType.CapEx_Acquisition),
            Line(TransactionLineType.Credit, BankId, AccountType.BankAccount, null)
        ]);
        missingProperty.Should().Throw<DomainException>();

        TransactionPostingRules.Ensure(TransactionType.PropertyPurchase, Guid.NewGuid(), [
            Line(TransactionLineType.Debit, null, null, CategoryType.CapEx_Acquisition),
            Line(TransactionLineType.Credit, BankId, AccountType.BankAccount, null)
        ]);
    }

    [Fact]
    public void Reversal_CannotBeReversed()
    {
        var userId = Guid.NewGuid();
        var original = Transaction.Create(userId, new DateOnly(2026, 6, 1), TransactionType.Expense, 100m, "Groceries", userId);
        original.AddLine(TransactionLineType.Debit, 100m, categoryId: Guid.NewGuid());
        original.AddLine(TransactionLineType.Credit, 100m, accountId: BankId);
        original.Post();
        var reversal = original.CreateReversal(userId, "Wrong amount");
        reversal.Post();

        var act = () => reversal.CreateReversal(userId, "Undo the void");
        act.Should().Throw<DomainException>();
    }

    private static PostingLine Line(TransactionLineType lineType, Guid? accountId, AccountType? accountType, CategoryType? categoryType)
        => new(lineType, 100m, accountId, accountType, null, categoryType);
}
