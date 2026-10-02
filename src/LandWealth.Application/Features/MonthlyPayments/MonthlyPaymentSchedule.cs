namespace LandWealth.Application.Features.MonthlyPayments;

public static class MonthlyPaymentSchedule
{
    public static bool IsDue(DateOnly startsOn, DateOnly? stoppedOn, int? totalInstallments, bool isActive, int year, int month)
    {
        if (month is < 1 or > 12)
            return false;

        var cursor = year * 12 + month;
        var start = startsOn.Year * 12 + startsOn.Month;
        if (cursor < start)
            return false;

        if (totalInstallments is int total && cursor - start >= total)
            return false;

        if (!isActive)
        {
            if (stoppedOn is null)
                return false;
            var stop = stoppedOn.Value.Year * 12 + stoppedOn.Value.Month;
            if (cursor > stop)
                return false;
        }

        return true;
    }

    public static DateOnly DueDate(int year, int month, int dueDay)
    {
        var day = Math.Clamp(dueDay, 1, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, day);
    }
}
