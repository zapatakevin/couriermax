namespace CourierMax.Domain.Enums;

/// <summary>
/// Estados válidos que puede tener un envío a lo largo de su ciclo de vida.
/// CREADO → ASIGNADO → EN_TRANSITO → ENTREGADO
///                    ↓
///              CANCELADO (desde cualquier estado, excepto ENTREGADO)
/// </summary>
public enum ShipmentStatus
{
    CREADO = 0,
    ASIGNADO = 1,
    EN_TRANSITO = 2,
    ENTREGADO = 3,
    CANCELADO = 4
}
