using CourierMax.Domain.Exceptions;

namespace CourierMax.Domain.ValueObjects;

/// <summary>Dirección postal. No puede estar vacía.</summary>
public sealed record Address
{
    public string Value { get; }

    private Address(string value) => Value = value.Trim();

    public static Address Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Trim().Length < 5)
            throw new ValidationException(nameof(Address), "La dirección es obligatoria y debe tener al menos 5 caracteres.");
        return new Address(raw);
    }

    public override string ToString() => Value;
}
