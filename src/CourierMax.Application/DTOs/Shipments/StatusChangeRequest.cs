namespace CourierMax.Application.DTOs.Shipments;

public sealed record StartTransitRequest(string ActorId);
public sealed record MarkDeliveredRequest(string ActorId);
public sealed record CancelShipmentRequest(string Reason, string ActorId);
