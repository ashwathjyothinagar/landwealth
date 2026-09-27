using FluentAssertions;
using LandWealth.Application.Common;
using LandWealth.Domain.Accounting;
using LandWealth.Domain.Entities;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;
using LandWealth.Domain.ValueObjects;

namespace LandWealth.UnitTests;

public class PropertyAccountingTests
{
    [Fact]
    public void CostBasis_IncludesCapEx_AndIgnoresOperatingExpenses()
    {
        var propertyId = Guid.NewGuid();
        var lines = new[]
        {
            new BasisLine(propertyId, TransactionLineType.Debit, 200000m, CategoryType.CapEx_Acquisition),
            new BasisLine(propertyId, TransactionLineType.Debit, 50000m, CategoryType.CapEx_Improvement),
            new BasisLine(propertyId, TransactionLineType.Debit, 8000m, CategoryType.OpEx_Maintenance),
            new BasisLine(propertyId, TransactionLineType.Credit, 25000m, CategoryType.CapEx_Acquisition)
        };

        PropertyAccounting.CostBasis(lines, propertyId).Should().Be(225000m);
        var summary = PropertyAccounting.Summarize(lines, propertyId);
        summary.Acquisition.Should().Be(175000m);
        summary.Improvements.Should().Be(50000m);
        summary.Maintenance.Should().Be(8000m);
        summary.CostBasis.Should().Be(summary.Acquisition + summary.Improvements);
    }

    [Fact]
    public void UnrealizedGain_UsesValuationWithoutTreatingItAsCash()
    {
        PropertyAccounting.UnrealizedGain(350000m, 225000m).Should().Be(125000m);
        PropertyAccounting.UnrealizedGain(null, 225000m).Should().BeNull();
    }

    [Fact]
    public void PartialSale_AllocatesCostBasisProRata()
    {
        var allocated = PropertyAccounting.AllocatedCostBasis(300000m, 1m, 3m);
        allocated.Should().Be(100000m);
        PropertyAccounting.RealizedGain(180000m, 5000m, allocated).Should().Be(75000m);

        var activeAcres = new Extent(2m, ExtentUnit.Acres).InAcres + new Extent(40m, ExtentUnit.Guntas).InAcres;
        activeAcres.Should().Be(3m);
        PropertyAccounting.AllocatedCostBasis(225000m, new Extent(1m, ExtentUnit.Acres).InAcres, activeAcres).Should().Be(75000m);
    }

    [Fact]
    public void LiquidNetWorth_SubtractsCreditCardBalances()
    {
        var liquid = PropertyAccounting.LiquidNetWorth([
            (AccountType.BankAccount, 100000m),
            (AccountType.CashAccount, 5000m),
            (AccountType.CreditCard, 12000m)
        ]);

        liquid.Should().Be(93000m);
    }

    [Fact]
    public void CashFlow_IgnoresTransfers()
    {
        var flow = PropertyAccounting.CashFlow([
            new CashLine(AccountType.BankAccount, TransactionLineType.Debit, 40000m, TransactionType.Income),
            new CashLine(AccountType.BankAccount, TransactionLineType.Credit, 15000m, TransactionType.Expense),
            new CashLine(AccountType.BankAccount, TransactionLineType.Credit, 10000m, TransactionType.Transfer),
            new CashLine(AccountType.BankAccount, TransactionLineType.Debit, 10000m, TransactionType.Transfer)
        ]);

        flow.Inflows.Should().Be(40000m);
        flow.Outflows.Should().Be(15000m);
    }

    [Fact]
    public void Ownership_CannotExceedOneHundredPercent()
    {
        var userId = Guid.NewGuid();
        var property = Property.Create(userId, "Chitnahalli", PropertyType.AgriculturalLand, "Karnataka", userId);
        property.AddOwner(PropertyOwner.Create(property.Id, userId, "Ashwath", 60m, OwnershipType.TenancyInCommon, new DateOnly(2024, 1, 1), userId));

        var act = () => property.AddOwner(PropertyOwner.Create(
            property.Id, userId, "Sibling", 50m, OwnershipType.TenancyInCommon, new DateOnly(2024, 1, 1), userId));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SubdivideParcel_SplitsExtentAndRetiresTheParent()
    {
        var userId = Guid.NewGuid();
        var property = Property.Create(userId, "Chitnahalli", PropertyType.AgriculturalLand, "Karnataka", userId);
        var parcel = property.AddParcel("42", 3m, ExtentUnit.Acres, userId);

        var (remainder, split) = property.SubdivideParcel(parcel.Id, 1m, "1", "2", userId);

        parcel.Status.Should().Be(ParcelStatus.Subdivided);
        remainder.Extent.Value.Should().Be(2m);
        split.Extent.Value.Should().Be(1m);
        remainder.Status.Should().Be(ParcelStatus.Active);
    }

    [Fact]
    public void BankDebit_IncreasesCash_AndCreditCardCredit_IncreasesDebt()
    {
        var userId = Guid.NewGuid();
        var bank = Account.Create(userId, "HDFC", AccountType.BankAccount, 10000m, userId, lastFourDigits: "5521");
        bank.ApplyJournalLine(TransactionLineType.Credit, 2500m);
        bank.CurrentBalance.Should().Be(7500m);

        var card = Account.Create(userId, "HDFC Card", AccountType.CreditCard, 0m, userId, lastFourDigits: "4821");
        card.ApplyJournalLine(TransactionLineType.Credit, 3000m);
        card.CurrentBalance.Should().Be(3000m);
        card.MaskedAccountNumber.Should().Be("•••• 4821");

        bank.UpdateDetails(bank.Name, bank.Institution, false, bank.Notes, userId);
        var closed = () => bank.ApplyJournalLine(TransactionLineType.Debit, 100m);
        closed.Should().Throw<DomainException>();

        var replay = AccountLedger.BalancesAfter(AccountType.CreditCard, 0m, [
            (TransactionLineType.Credit, 3000m),
            (TransactionLineType.Debit, 500m)
        ]);
        replay.Should().Equal(3000m, 2500m);
    }

    [Fact]
    public void ReminderPolicy_EscalatesOverduePriority()
    {
        ReminderPolicy.Escalate(ReminderPriority.Low, true).Should().Be(ReminderPriority.High);
        ReminderPolicy.DisplayStatus(ReminderStatus.Pending, true).Should().Be(ReminderStatus.Overdue);
        ReminderPolicy.DisplayStatus(ReminderStatus.Completed, true).Should().Be(ReminderStatus.Completed);
        ReminderPolicy.ShowOnDashboard(ReminderStatus.Pending, new DateOnly(2026, 9, 25), new DateOnly(2026, 9, 26)).Should().BeTrue();
        ReminderPolicy.ShowOnDashboard(ReminderStatus.Pending, new DateOnly(2026, 10, 20), new DateOnly(2026, 9, 26)).Should().BeTrue();
        ReminderPolicy.ShowOnDashboard(ReminderStatus.Pending, new DateOnly(2026, 11, 15), new DateOnly(2026, 9, 26)).Should().BeFalse();
        ReminderPolicy.ShowOnDashboard(ReminderStatus.Completed, new DateOnly(2026, 9, 25), new DateOnly(2026, 9, 26)).Should().BeFalse();
    }

    [Fact]
    public void FileSignatures_AcceptPdfAndRejectMismatchedBytes()
    {
        var pdf = "%PDF-1.4\n"u8.ToArray();
        FileSignatures.Matches("application/pdf", pdf).Should().BeTrue();
        FileSignatures.Matches("image/png", pdf).Should().BeFalse();
    }

    [Fact]
    public void PlannedProperty_CanMoveToPurchasedOrArchivedOnly()
    {
        var userId = Guid.NewGuid();
        var property = Property.Create(userId, "Plot", PropertyType.ResidentialLand, "Karnataka", userId);
        property.NextStatuses().Should().BeEquivalentTo([PropertyStatus.Purchased, PropertyStatus.Archived]);
    }

    [Fact]
    public void PropertyLifecycle_RejectsArchivedToHeld()
    {
        var userId = Guid.NewGuid();
        var property = Property.Create(userId, "Plot", PropertyType.ResidentialLand, "Karnataka", userId);
        property.Transition(PropertyStatus.Archived, userId);

        var act = () => property.Transition(PropertyStatus.Held, userId);
        act.Should().Throw<DomainException>();
    }
}
