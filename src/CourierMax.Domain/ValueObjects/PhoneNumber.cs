using System.Text.RegularExpressions;
using CourierMax.Domain.Exceptions;

namespace CourierMax.Domain.ValueObjects;

/// <summary>
/// Teléfono colombiano: 10 dígitos, inicia con 3 (móvil) o 6 (fijo).
/// </summary>
public sealed record PhoneNumber
{
    private static readonly Regex Format = new(@"^[36]\d{9}$", RegexOptions.Compiled);

    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    public static PhoneNumber Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ValidationException(nameof(PhoneNumber), "El teléfono es obligatorio.");
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (!Format.IsMatch(digits))
            throw new ValidationException(nameof(PhoneNumber),
                "El teléfono debe tener 10 dígitos e iniciar con 3 (móvil) o 6 (fijo).");
        return new PhoneNumber(digits);
    }

    public override string ToString() => Value;
}
