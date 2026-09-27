using FluentAssertions;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Events;
using LandWealth.Domain.Exceptions;
using LandWealth.Domain.ValueObjects;

namespace LandWealth.UnitTests;

public class DomainModelTests
{
    [Fact]
    public void Money_Add_RequiresTheSameCurrency()
    {
        var rupees = Money.Inr(1500.25m);
        var dollars = new Money(10m, "USD");

        var sum = rupees.Add(Money.Inr(499.75m));
        sum.Amount.Should().Be(2000m);
        sum.Currency.Should().Be("INR");

        Money.Inr(1234567.50m).Add(Money.Inr(0.50m)).Amount.Should().Be(1234568.00m);

        var act = () => rupees.Add(dollars);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Extent_ConvertsGuntasToAcres_WithoutChangingTheStoredUnit()
    {
        var extent = new Extent(40m, ExtentUnit.Guntas);

        extent.Value.Should().Be(40m);
        extent.Unit.Should().Be(ExtentUnit.Guntas);
        extent.InAcres.Should().Be(1m);
    }

    [Fact]
    public void Extent_RejectsNonPositiveValues()
    {
        var act = () => new Extent(0m, ExtentUnit.Acres);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Account_StoresOnlyTheLastFourDigits()
    {
        var account = Account.Create(
            Guid.NewGuid(),
            "HDFC Bank Savings",
            AccountType.BankAccount,
            25000m,
            Guid.NewGuid(),
            institution: "HDFC Bank",
            lastFourDigits: "4821");

        account.MaskedAccountNumber.Should().Be("•••• 4821");
        account.CurrentBalance.Should().Be(25000m);

        var act = () => Account.Create(
            Guid.NewGuid(), "Cash", AccountType.CashAccount, 0m, Guid.NewGuid(), lastFourDigits: "48219");
        act.Should().Throw<DomainException>();

        var missingMask = () => Account.Create(
            Guid.NewGuid(), "SBI", AccountType.BankAccount, 0m, Guid.NewGuid());
        missingMask.Should().Throw<DomainException>();

        var cash = Account.Create(Guid.NewGuid(), "Petty cash", AccountType.CashAccount, 500m, Guid.NewGuid());
        cash.MaskedAccountNumber.Should().BeNull();
        cash.CurrentBalance.Should().Be(500m);
    }

    [Fact]
    public void PropertyParcel_RejectsOwnershipAboveOneHundredPercent()
    {
        var act = () => PropertyParcel.Create(
            Guid.NewGuid(), Guid.NewGuid(), "42", 2.5m, ExtentUnit.Acres, Guid.NewGuid(), ownershipPercentage: 100.01m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Transaction_Post_RequiresBalancedLines_AndRaisesDomainEvent()
    {
        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(
            userId,
            new DateOnly(2026, 4, 1),
            TransactionType.PropertyExpense,
            50000m,
            "Solar fencing at Chitnahalli",
            userId,
            propertyId: Guid.NewGuid());

        transaction.AddLine(TransactionLineType.Debit, 50000m, propertyId: transaction.PropertyId);
        transaction.AddLine(TransactionLineType.Credit, 40000m, accountId: Guid.NewGuid());

        var unbalanced = () => transaction.Post();
        unbalanced.Should().Throw<DomainException>();

        transaction.AddLine(TransactionLineType.Credit, 10000m, accountId: Guid.NewGuid());
        transaction.Post();

        transaction.Status.Should().Be(TransactionStatus.Posted);
        transaction.IsBalanced.Should().BeTrue();
        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TransactionPostedEvent>();
    }

    [Fact]
    public void Transaction_CreateReversal_MirrorsDebitAndCreditLines()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var transaction = Transaction.Create(
            userId, new DateOnly(2026, 4, 1), TransactionType.PropertyPurchase, 100000m, "Purchase", userId, propertyId);
        transaction.AddLine(TransactionLineType.Debit, 100000m, propertyId: propertyId);
        transaction.AddLine(TransactionLineType.Credit, 100000m, accountId: accountId);
        transaction.Post();

        var reversal = transaction.CreateReversal(userId, "Reverse purchase");

        reversal.TransactionType.Should().Be(TransactionType.Reversal);
        reversal.ReversedTransactionId.Should().Be(transaction.Id);
        reversal.IsBalanced.Should().BeTrue();
        reversal.Lines.Should().ContainSingle(line => line.LineType == TransactionLineType.Credit && line.PropertyId == propertyId);
        reversal.Lines.Should().ContainSingle(line => line.LineType == TransactionLineType.Debit && line.AccountId == accountId);
    }

    [Fact]
    public void Property_RejectsANegativePurchasePrice()
    {
        var userId = Guid.NewGuid();
        var property = Property.Create(userId, "Chitnahalli Farm", PropertyType.AgriculturalLand, "Karnataka", userId);

        var act = () => property.UpdateDetails(
            "Chitnahalli Farm",
            PropertyType.AgriculturalLand,
            PropertyStatus.Purchased,
            "42",
            new DateOnly(2024, 6, 1),
            -1m,
            null, "Chitnahalli", null, "Hassan", "Karnataka", "India", null, null, null, null,
            userId);

        act.Should().Throw<DomainException>();
    }
}
