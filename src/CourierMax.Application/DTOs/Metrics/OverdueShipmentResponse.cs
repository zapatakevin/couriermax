using CourierMax.Application.DTOs.Shipments;

namespace CourierMax.Application.DTOs.Metrics;

public sealed record OverdueShipmentResponse(ShipmentResponse Shipment, int BusinessDaysSinceCreation, int SlaBusinessDays);
