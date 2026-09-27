using FluentAssertions;
using LandWealth.Domain.Accounting;
using LandWealth.Domain.Enums;

namespace LandWealth.UnitTests;

public class ValuationPolicyTests
{
    [Fact]
    public void EstimatedValue_PrefersMarket_AndKeepsGuidanceSeparate()
    {
        var history = new[]
        {
            new ValuationSnapshot(new DateOnly(2026, 1, 1), 350000m, ValuationSource.LocalMarketSurvey, new DateTime(2026, 1, 1)),
            new ValuationSnapshot(new DateOnly(2026, 6, 1), 180000m, ValuationSource.GovernmentGuidanceValue, new DateTime(2026, 6, 1))
        };

        ValuationPolicy.LatestMarket(history)!.Value.Amount.Should().Be(350000m);
        ValuationPolicy.LatestGuidance(history)!.Value.Amount.Should().Be(180000m);
        ValuationPolicy.EstimatedValue(history).Should().Be(350000m);
        PropertyAccounting.UnrealizedGain(ValuationPolicy.EstimatedValue(history), 100000m).Should().Be(250000m);
    }

    [Fact]
    public void EstimatedValue_UsesGuidanceWhenNoMarketEstimateExists()
    {
        var history = new[]
        {
            new ValuationSnapshot(new DateOnly(2026, 6, 1), 180000m, ValuationSource.GovernmentGuidanceValue, new DateTime(2026, 6, 1))
        };

        ValuationPolicy.EstimatedValue(history).Should().Be(180000m);
        ValuationPolicy.LatestMarket(history).Should().BeNull();
    }

    [Fact]
    public void SameDayMarketEstimate_UsesTheLaterRecord()
    {
        var history = new[]
        {
            new ValuationSnapshot(new DateOnly(2026, 7, 1), 300000m, ValuationSource.OwnerEstimate, new DateTime(2026, 7, 1, 8, 0, 0)),
            new ValuationSnapshot(new DateOnly(2026, 7, 1), 400000m, ValuationSource.BankValuation, new DateTime(2026, 7, 1, 16, 0, 0))
        };

        ValuationPolicy.LatestMarket(history)!.Value.Amount.Should().Be(400000m);
    }
}
