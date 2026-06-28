using System.Text.RegularExpressions;
using CourierMax.Domain.Exceptions;

namespace CourierMax.Domain.ValueObjects;

/// <summary>
/// Código de rastreo único con formato CM-XXXXXXXX (8 dígitos).
/// </summary>
public sealed record TrackingCode
{
    private static readonly Regex Format = new(@"^CM-\d{8}$", RegexOptions.Compiled);

    public string Value { get; }

    private TrackingCode(string value) => Value = value;

    public static TrackingCode Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !Format.IsMatch(raw))
            throw new ValidationException(nameof(TrackingCode),
                "El código de rastreo debe tener el formato CM-XXXXXXXX (8 dígitos).");
        return new TrackingCode(raw);
    }

    /// <summary>Genera un código nuevo. NO garantiza unicidad — esa validación es repositorio.</summary>
    public static TrackingCode Generate(Random rng)
    {
        var n = rng.Next(0, 100_000_000);
        return new TrackingCode($"CM-{n:D8}");
    }

    public override string ToString() => Value;
}
