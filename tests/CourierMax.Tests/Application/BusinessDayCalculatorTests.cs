using CourierMax.Application.Services;
using FluentAssertions;
using Xunit;

namespace CourierMaxTests;

public class BusinessDayCalculatorTests
{
    private readonly ColombianBusinessDayCalculator _calc = new();

    [Fact]
    public void Friday_to_monday_counts_as_one_business_day()
    {
        // viernes 2026-06-12 → lunes 2026-06-15 (RN-02 example)
        var from = new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        _calc.BusinessDaysBetween(from, to).Should().Be(1);
    }

    [Fact]
    public void Saturday_and_sunday_are_excluded()
    {
        var sat = new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc);
        var sun = new DateTime(2026, 6, 14, 23, 59, 0, DateTimeKind.Utc);
        _calc.BusinessDaysBetween(sat, sun).Should().Be(0);
    }

    [Fact]
    public void Colombian_holidays_are_excluded()
    {
        // 1 de mayo 2026 (festivo) - viernes
        var from = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc);
        // hábiles en el rango: viernes 1 (festivo) y lunes 4 → solo lunes 4 = 1 hábil
        _calc.BusinessDaysBetween(from, to).Should().Be(1);
    }

    [Fact]
    public void Add_business_days_skips_weekend()
    {
        var from = new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc); // viernes
        var result = _calc.AddBusinessDays(from, 1); // debe caer lunes 15
        result.Should().Be(new DateTime(2026, 6, 15));
    }

    [Fact]
    public void Same_day_business_day_returns_zero()
    {
        var d = new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc); // martes
        _calc.BusinessDaysBetween(d, d).Should().Be(0);
    }
}
