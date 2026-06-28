using CourierMax.Domain.Exceptions;

namespace CourierMax.Domain.ValueObjects;

/// <summary>
/// Dimensiones físicas de un paquete en centímetros.
/// Cada lado: 1..200 cm. RN-04.
/// Modelado como owned entity de <see cref="Shipment"/>.
/// </summary>
public sealed class PackageDimensions
{
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }

    public PackageDimensions() { }

    public PackageDimensions(decimal length, decimal width, decimal height)
    {
        ValidateSide(length, nameof(length));
        ValidateSide(width, nameof(width));
        ValidateSide(height, nameof(height));
        Length = length;
        Width = width;
        Height = height;
    }

    public static PackageDimensions Create(decimal length, decimal width, decimal height) =>
        new(length, width, height);

    private static void ValidateSide(decimal v, string name)
    {
        if (v < 1m || v > 200m)
            throw new ValidationException(name, $"{name} debe estar entre 1 y 200 cm.");
    }

    /// <summary>Volumen en m³ (cm³ / 1_000_000).</summary>
    public decimal VolumeM3() => (Length * Width * Height) / 1_000_000m;
}
