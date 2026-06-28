using CourierMax.Application.DTOs.Common;
using CourierMax.Domain.Enums;

namespace CourierMax.Application.DTOs.Shipments;

public sealed record CreateShipmentRequest(
    ContactDto Sender,
    ContactDto Recipient,
    decimal WeightKg,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    PackageType PackageType,
    ServiceType ServiceType,
    string OriginCity,
    string DestinationCity);
