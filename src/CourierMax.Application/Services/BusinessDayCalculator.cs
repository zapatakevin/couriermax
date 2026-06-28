namespace CourierMax.Application.Services;

/// <summary>Calcula días hábiles (sin sábados, domingos ni festivos colombianos).</summary>
public interface IBusinessDayCalculator
{
    int BusinessDaysBetween(DateTime fromUtc, DateTime toUtc);
    DateTime AddBusinessDays(DateTime fromUtc, int businessDays);
}

public sealed class ColombianBusinessDayCalculator : IBusinessDayCalculator
{
    // Festivos colombianos 2026 (RN-02)
    private static readonly HashSet<DateOnly> Holidays = new()
    {
        new(2026, 1, 1), new(2026, 1, 26), new(2026, 1, 30),
        new(2026, 3, 24), new(2026, 5, 1), new(2026, 6, 1),
        new(2026, 6, 29), new(2026, 7, 20), new(2026, 8, 17),
        new(2026, 10, 20), new(2026, 11, 9), new(2026, 12, 8)
    };

    public int BusinessDaysBetween(DateTime fromUtc, DateTime toUtc)
    {
        if (toUtc < fromUtc) return 0;
        var from = DateOnly.FromDateTime(fromUtc.Date);
        var to = DateOnly.FromDateTime(toUtc.Date);
        int count = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (Holidays.Contains(d)) continue;
            count++;
        }
        // El conteo anterior incluye AMBOS extremos. Para la diferencia "entre" días
        // debemos excluir el día inicial. Si el día inicial es hábil, restamos 1.
        if (IsBusinessDay(from)) count--;
        return Math.Max(0, count);
    }

    public DateTime AddBusinessDays(DateTime fromUtc, int businessDays)
    {
        var d = DateOnly.FromDateTime(fromUtc.Date);
        int added = 0;
        while (added < businessDays)
        {
            d = d.AddDays(1);
            if (IsBusinessDay(d)) added++;
        }
        return d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    private static bool IsBusinessDay(DateOnly d) =>
        d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !Holidays.Contains(d);
}
