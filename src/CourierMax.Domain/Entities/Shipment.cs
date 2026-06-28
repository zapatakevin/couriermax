using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;
using CourierMax.Domain.ValueObjects;

namespace CourierMax.Domain.Entities;

/// <summary>
/// Aggregate Root del ciclo de vida del envío.
/// Encapsula el flujo de estados, asignación y liberación de capacidad.
/// </summary>
public sealed class Shipment : Entity
{
    public string TrackingCode { get; private set; } = default!;
    public string SenderName { get; private set; } = default!;
    public string SenderPhone { get; private set; } = default!;
    public string SenderAddress { get; private set; } = default!;
    public string RecipientName { get; private set; } = default!;
    public string RecipientPhone { get; private set; } = default!;
    public string RecipientAddress { get; private set; } = default!;
    public decimal WeightKg { get; private set; }
    public PackageDimensions Dimensions { get; private set; } = default!;
    public PackageType PackageType { get; private set; }
    public ServiceType ServiceType { get; private set; }
    public long OriginCityId { get; private set; }
    public long DestinationCityId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public decimal BaseFee { get; private set; }
    public decimal WeightFee { get; private set; }
    public decimal DistanceFee { get; private set; }
    public decimal PackageSurcharge { get; private set; }
    public decimal TotalFee { get; private set; }
    public long? AssignedVehicleId { get; private set; }
    public long? AssignedDriverId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    private readonly List<StateTransition> _transitions = new();
    public IReadOnlyList<StateTransition> Transitions => _transitions.AsReadOnly();

    private Shipment() { }

    public Shipment(
        TrackingCode trackingCode,
        string senderName, PhoneNumber senderPhone, Address senderAddress,
        string recipientName, PhoneNumber recipientPhone, Address recipientAddress,
        decimal weightKg, PackageDimensions dimensions, PackageType packageType, ServiceType serviceType,
        long originCityId, long destinationCityId,
        TariffBreakdown tariff,
        DateTime now)
    {
        if (weightKg < 0.1m || weightKg > 100m)
            throw new ValidationException(nameof(weightKg), "El peso debe estar entre 0.1 y 100 kg.");
        if (originCityId == destinationCityId)
            throw new BusinessRuleException("ORIGIN_EQUALS_DEST", "Origen y destino no pueden ser iguales.");

        TrackingCode = trackingCode.Value;
        SenderName = senderName;
        SenderPhone = senderPhone.Value;
        SenderAddress = senderAddress.Value;
        RecipientName = recipientName;
        RecipientPhone = recipientPhone.Value;
        RecipientAddress = recipientAddress.Value;
        WeightKg = weightKg;
        Dimensions = dimensions;
        PackageType = packageType;
        ServiceType = serviceType;
        OriginCityId = originCityId;
        DestinationCityId = destinationCityId;
        Status = ShipmentStatus.CREADO;
        CreatedAt = now;

        ApplyTariff(tariff);

        _transitions.Add(new StateTransition(null, ShipmentStatus.CREADO, "Creación del envío", "system", now));
    }

    private void ApplyTariff(TariffBreakdown t)
    {
        BaseFee = t.BaseFee;
        WeightFee = t.WeightFee;
        DistanceFee = t.DistanceFee;
        PackageSurcharge = t.PackageSurcharge;
        TotalFee = t.Total;
    }

    /// <summary>Asigna el envío a un vehículo/conductor. Verifica capacidad externamente (servicio de dominio).</summary>
    public void AssignTo(long vehicleId, long driverId, DateTime now)
    {
        EnsureStatus(ShipmentStatus.CREADO, "solo se puede asignar un envío en CREADO");
        Status = ShipmentStatus.ASIGNADO;
        AssignedVehicleId = vehicleId;
        AssignedDriverId = driverId;
        AssignedAt = now;
        _transitions.Add(new StateTransition(ShipmentStatus.CREADO, ShipmentStatus.ASIGNADO,
            "Asignación a vehículo/conductor", driverId.ToString(), now));
    }

    public void StartTransit(DateTime now, string actorId)
    {
        EnsureStatus(ShipmentStatus.ASIGNADO, "solo se inicia tránsito desde ASIGNADO");
        Status = ShipmentStatus.EN_TRANSITO;
        _transitions.Add(new StateTransition(ShipmentStatus.ASIGNADO, ShipmentStatus.EN_TRANSITO,
            "Inicio de tránsito", actorId, now));
    }

    public void MarkDelivered(DateTime now, string actorId)
    {
        if (Status == ShipmentStatus.ENTREGADO)
            throw new BusinessRuleException("ALREADY_DELIVERED", "El envío ya fue entregado.");
        EnsureStatus(ShipmentStatus.EN_TRANSITO, "solo se marca ENTREGADO desde EN_TRANSITO");
        Status = ShipmentStatus.ENTREGADO;
        DeliveredAt = now;
        _transitions.Add(new StateTransition(ShipmentStatus.EN_TRANSITO, ShipmentStatus.ENTREGADO,
            "Entrega completada", actorId, now));
    }

    /// <summary>Cancela el envío (no permitido si ya fue ENTREGADO). Libera capacidad externamente.</summary>
    public void Cancel(string reason, string actorId, DateTime now)
    {
        if (Status == ShipmentStatus.ENTREGADO)
            throw new BusinessRuleException("CANNOT_CANCEL_DELIVERED",
                "No se puede cancelar un envío que ya fue entregado.");
        if (Status == ShipmentStatus.CANCELADO)
            throw new BusinessRuleException("ALREADY_CANCELLED", "El envío ya está cancelado.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            throw new ValidationException(nameof(reason), "El motivo de cancelación debe tener al menos 5 caracteres.");

        var previous = Status;
        Status = ShipmentStatus.CANCELADO;
        AssignedVehicleId = null;
        AssignedDriverId = null;
        _transitions.Add(new StateTransition(previous, ShipmentStatus.CANCELADO, reason.Trim(), actorId, now));
    }

    /// <summary>Recalcula la tarifa del envío si las reglas tributarias cambiaran. Sólo permitido en CREADO.</summary>
    public void RecalculateTariff(TariffBreakdown tariff)
    {
        EnsureStatus(ShipmentStatus.CREADO, "solo se recalcula tarifa en CREADO");
        ApplyTariff(tariff);
    }

    public decimal VolumeM3() => Dimensions.VolumeM3();

    private void EnsureStatus(ShipmentStatus expected, string message)
    {
        if (Status != expected)
            throw new BusinessRuleException("INVALID_STATE",
                $"Estado inválido: se esperaba {expected} pero se encontró {Status}. {message}.");
    }
}

/// <summary>Desglose de la tarifa calculada (RF-04).</summary>
public sealed record TariffBreakdown(
    decimal BaseFee,
    decimal WeightFee,
    decimal DistanceFee,
    decimal PackageSurcharge,
    decimal Total);

/// <summary>Cambio de estado inmutable (RF-02). PK Guid para evitar colisiones cuando
/// varios transitions del mismo envío son añadidos en una sola transacción.</summary>
public sealed class StateTransition
{
    public Guid Id { get; private set; }
    public ShipmentStatus? FromStatus { get; private set; }
    public ShipmentStatus ToStatus { get; private set; }
    public string Reason { get; private set; } = default!;
    public string ActorId { get; private set; } = default!;
    public DateTime ChangedAt { get; private set; }

    private StateTransition() { }

    public StateTransition(ShipmentStatus? from, ShipmentStatus to, string reason, string actorId, DateTime at)
    {
        // Id se asignará por la base de datos (ValueGeneratedOnAdd).
        FromStatus = from;
        ToStatus = to;
        Reason = reason;
        ActorId = actorId;
        ChangedAt = at;
    }
}
