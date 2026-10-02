using FluentAssertions;
using LandWealth.Application.Features.MonthlyPayments;

namespace LandWealth.UnitTests;

public class MonthlyPaymentScheduleTests
{
    [Fact]
    public void Bill_IsDueFromTheStartMonth_UntilItIsStopped()
    {
        var start = new DateOnly(2026, 3, 1);
        MonthlyPaymentSchedule.IsDue(start, null, null, true, 2026, 2).Should().BeFalse();
        MonthlyPaymentSchedule.IsDue(start, null, null, true, 2026, 3).Should().BeTrue();
        MonthlyPaymentSchedule.IsDue(start, new DateOnly(2026, 6, 2), null, false, 2026, 6).Should().BeTrue();
        MonthlyPaymentSchedule.IsDue(start, new DateOnly(2026, 6, 2), null, false, 2026, 7).Should().BeFalse();
    }

    [Fact]
    public void Emi_StopsAfterTheInstallmentCount()
    {
        var start = new DateOnly(2026, 1, 5);
        MonthlyPaymentSchedule.IsDue(start, null, 3, true, 2026, 1).Should().BeTrue();
        MonthlyPaymentSchedule.IsDue(start, null, 3, true, 2026, 3).Should().BeTrue();
        MonthlyPaymentSchedule.IsDue(start, null, 3, true, 2026, 4).Should().BeFalse();
    }

    [Fact]
    public void DueDate_ClampsToTheLastDayOfAShortMonth()
    {
        MonthlyPaymentSchedule.DueDate(2026, 2, 31).Should().Be(new DateOnly(2026, 2, 28));
    }
}
