namespace CourierMax.Domain.Enums;

/// <summary>
/// Tipo de servicio contratado por el remitente.
/// Define la tarifa base y el SLA en días hábiles.
/// </summary>
public enum ServiceType
{
    /// <summary>3-5 días hábiles. SLA: 5 días hábiles.</summary>
    Estandar = 0,
    /// <summary>1-2 días hábiles. SLA: 2 días hábiles.</summary>
    Express = 1,
    /// <summary>Mismo día. SLA: 0 días hábiles (debe entregarse el mismo día).</summary>
    MismoDia = 2
}
