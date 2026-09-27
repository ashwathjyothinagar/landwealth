using LandWealth.Domain.Enums;

namespace LandWealth.Domain.Accounting;

public readonly record struct ValuationSnapshot(DateOnly Date, decimal Amount, ValuationSource Source, DateTime RecordedAt);

public static class ValuationPolicy
{
    public static bool IsGuidance(ValuationSource source) => source == ValuationSource.GovernmentGuidanceValue;

    public static ValuationSnapshot? LatestGuidance(IEnumerable<ValuationSnapshot> history)
        => Latest(history, guidance: true);

    public static ValuationSnapshot? LatestMarket(IEnumerable<ValuationSnapshot> history)
        => Latest(history, guidance: false);

    /// <summary>
    /// Unrealized gain uses a market estimate. A guidance value is used only when no market estimate exists.
    /// </summary>
    public static decimal? EstimatedValue(IEnumerable<ValuationSnapshot> history)
        => LatestMarket(history)?.Amount ?? LatestGuidance(history)?.Amount;

    private static ValuationSnapshot? Latest(IEnumerable<ValuationSnapshot> history, bool guidance)
    {
        ValuationSnapshot? chosen = null;
        foreach (var row in history)
        {
            if (IsGuidance(row.Source) != guidance)
                continue;
            if (chosen is null || row.Date > chosen.Value.Date || (row.Date == chosen.Value.Date && row.RecordedAt >= chosen.Value.RecordedAt))
                chosen = row;
        }

        return chosen;
    }
}
