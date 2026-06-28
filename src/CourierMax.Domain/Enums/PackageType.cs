namespace CourierMax.Domain.Enums;

/// <summary>
/// Tipo de paquete. Determina el recargo porcentual sobre la tarifa.
/// </summary>
public enum PackageType
{
    Documento = 0,
    Paquete = 1,
    Fragil = 2,
    Perecedero = 3
}
