using CourierMax.Domain.Enums;

namespace CourierMax.Application.DTOs.Shipments;

public sealed record ShipmentResponse(
    long Id,
    string TrackingCode,
    string SenderName,
    string SenderPhone,
    string SenderAddress,
    string RecipientName,
    string RecipientPhone,
    string RecipientAddress,
    decimal WeightKg,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    PackageType PackageType,
    ServiceType ServiceType,
    string OriginCity,
    string DestinationCity,
    ShipmentStatus Status,
    decimal BaseFee,
    decimal WeightFee,
    decimal DistanceFee,
    decimal PackageSurcharge,
    decimal TotalFee,
    long? AssignedVehicleId,
    long? AssignedDriverId,
    DateTime CreatedAt,
    DateTime? AssignedAt,
    DateTime? DeliveredAt,
    IReadOnlyList<TransitionResponse> Transitions);

public sealed record TransitionResponse(
    ShipmentStatus? FromStatus,
    ShipmentStatus ToStatus,
    string Reason,
    string ActorId,
    DateTime ChangedAt);
